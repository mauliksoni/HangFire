// This file is part of HangFire.
// Copyright � 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using Dapper;
using HangFire.Common;
using HangFire.Server;
using HangFire.SqlServer.Entities;
using HangFire.Storage;

namespace HangFire.SqlServer
{
    internal class SqlServerConnection : IStorageConnection
    {
        private readonly SqlConnection _connection;
        private readonly SqlServerStorageOptions _options;

        public SqlServerConnection(SqlConnection connection, SqlServerStorageOptions options)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (options == null) throw new ArgumentNullException("options");

            _connection = connection;
            _options = options;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new SqlServerWriteOnlyTransaction(_connection);
        }

        public IDisposable AcquireJobLock(string jobId)
        {
            return new SqlServerDistributedLock(
                String.Format("HangFire:Job:{0}", jobId), 
                _connection);
        }

        public ProcessingJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");

            dynamic idAndQueue;

            const string fetchJobSqlTemplate = @"
set transaction isolation level read committed
update top (1) HangFire.JobQueue set FetchedAt = GETUTCDATE()
output INSERTED.JobId, INSERTED.Queue
where FetchedAt {0}
and Queue in @queues";

            // Sql query is splitted to force SQL Server to use 
            // INDEX SEEK instead of INDEX SCAN operator.
            var fetchConditions = new[] { "is null", "< DATEADD(second, @timeout, GETUTCDATE())" };
            var currentQueryIndex = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                idAndQueue = _connection.Query(
                    String.Format(fetchJobSqlTemplate, fetchConditions[currentQueryIndex]),
                    new { queues = queues, timeout = _options.InvisibilityTimeout.Negate().TotalSeconds })
                    .SingleOrDefault();

                if (idAndQueue == null)
                {
                    if (currentQueryIndex == fetchConditions.Length - 1)
                    {
                        cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                currentQueryIndex = (currentQueryIndex + 1) % fetchConditions.Length;
            } while (idAndQueue == null);

            return new ProcessingJob(
                idAndQueue.JobId.ToString(CultureInfo.InvariantCulture),
                idAndQueue.Queue);
        }

        public string CreateExpiredJob(
            Job job,
            IDictionary<string, string> parameters, 
            TimeSpan expireIn)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (parameters == null) throw new ArgumentNullException("parameters");

            const string createJobSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt)
values (@invocationData, @arguments, @createdAt, @expireAt);
SELECT CAST(SCOPE_IDENTITY() as int)";

            var invocationData = InvocationData.Serialize(job);

            var jobId = _connection.Query<int>(
                createJobSql,
                new
                {
                    invocationData = JobHelper.ToJson(invocationData),
                    arguments = invocationData.Arguments,
                    createdAt = DateTime.UtcNow,
                    expireAt = DateTime.UtcNow.Add(expireIn)
                }).Single().ToString();

            if (parameters.Count > 0)
            {
                var parameterArray = new object[parameters.Count];
                int parameterIndex = 0;
                foreach (var parameter in parameters)
                {
                    parameterArray[parameterIndex++] = new
                    {
                        jobId = jobId,
                        name = parameter.Key,
                        value = parameter.Value
                    };
                }

                const string insertParameterSql = @"
insert into HangFire.JobParameter (JobId, Name, Value)
values (@jobId, @name, @value)";

                _connection.Execute(insertParameterSql, parameterArray);
            }

            return jobId;
        }

        public JobData GetJobData(string id)
        {
            if (id == null) throw new ArgumentNullException("id");

            const string sql = 
                @"select InvocationData, StateName, Arguments from HangFire.Job where id = @id";

            var jobData = _connection.Query<SqlJob>(sql, new { id = id })
                .SingleOrDefault();

            if (jobData == null) return null;

            // TODO: conversion exception could be thrown.
            var invocationData = JobHelper.FromJson<InvocationData>(jobData.InvocationData);
            invocationData.Arguments = jobData.Arguments;

            Job job = null;
            JobLoadException loadException = null;

            try
            {
                job = invocationData.Deserialize();
            }
            catch (JobLoadException ex)
            {
                loadException = ex;
            }

            return new JobData
            {
                Job = job,
                State = jobData.StateName,
                LoadException = loadException
            };
        }

        public void SetJobParameter(string id, string name, string value)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (name == null) throw new ArgumentNullException("name");

            _connection.Execute(
                @"merge HangFire.JobParameter as Target "
                + @"using (VALUES (@jobId, @name, @value)) as Source (JobId, Name, Value) "
                + @"on Target.JobId = Source.JobId AND Target.Name = Source.Name "
                + @"when matched then update set Value = Source.Value "
                + @"when not matched then insert (JobId, Name, Value) values (Source.JobId, Source.Name, Source.Value);",
                new { jobId = id, name, value });
        }

        public string GetJobParameter(string id, string name)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (name == null) throw new ArgumentNullException("name");

            return _connection.Query<string>(
                @"select Value from HangFire.JobParameter where JobId = @id and Name = @name",
                new { id = id, name = name })
                .SingleOrDefault();
        }

        public void DeleteJobFromQueue(string id, string queue)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (queue == null) throw new ArgumentNullException("queue");

            _connection.Execute("delete from HangFire.JobQueue where JobId = @id and Queue = @queueName",
                new { id = id, queueName = queue });
        }

        public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (toScore < fromScore) throw new ArgumentException("The `toScore` value must be higher or equal to the `fromScore` value.");

            return _connection.Query<string>(
                @"select top 1 Value from HangFire.[Set] where [Key] = @key and Score between @from and @to order by Score",
                new { key, from = fromScore, to = toScore })
                .SingleOrDefault();
        }

        public void AnnounceServer(string serverId, ServerContext context)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");

            var data = new ServerData
            {
                WorkerCount = context.WorkerCount,
                Queues = context.Queues,
                StartedAt = DateTime.UtcNow,
            };

            _connection.Execute(
                @"merge HangFire.Server as Target "
                + @"using (VALUES (@id, @data, @heartbeat)) as Source (Id, Data, Heartbeat) "
                + @"on Target.Id = Source.Id "
                + @"when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat "
                + @"when not matched then insert (Id, Data, LastHeartbeat) values (Source.Id, Source.Data, Source.Heartbeat);",
                new { id = serverId, data = JobHelper.ToJson(data), heartbeat = DateTime.UtcNow });
        }

        public void RemoveServer(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");

            _connection.Execute(
                @"delete from HangFire.Server where Id = @id",
                new { id = serverId });
        }

        public void Heartbeat(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");

            _connection.Execute(
                @"update HangFire.Server set LastHeartbeat = @now where Id = @id",
                new { now = DateTime.UtcNow, id = serverId });
        }

        public int RemoveTimedOutServers(TimeSpan timeOut)
        {
            if (timeOut.Duration() != timeOut)
            {
                throw new ArgumentException("The `timeOut` value must be positive.", "timeOut");
            }

            return _connection.Execute(
                @"delete from HangFire.Server where LastHeartbeat < @timeOutAt",
                new { timeOutAt = DateTime.UtcNow.Add(timeOut.Negate()) });
        }
    }
}