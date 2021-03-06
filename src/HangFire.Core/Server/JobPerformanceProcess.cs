﻿// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Linq;
using HangFire.Common;

namespace HangFire.Server
{
    internal class JobPerformanceProcess : IJobPerformanceProcess
    {
        private readonly Func<Job, IEnumerable<JobFilter>> _getFiltersThunk 
            = JobFilterProviders.Providers.GetFilters;

        public JobPerformanceProcess()
        {
        }

        internal JobPerformanceProcess(IEnumerable<object> filters)
            : this()
        {
            if (filters != null)
            {
                _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
            }
        }

        public void Run(PerformContext context, IJobPerformer performer)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (performer == null) throw new ArgumentNullException("performer");

            var filterInfo = GetFilters(context.Job);

            try
            {
                PerformJobWithFilters(context, performer, filterInfo.ServerFilters);
            }
            catch (Exception ex)
            {
                var exceptionContext = new ServerExceptionContext(context, ex);
                InvokeServerExceptionFilters(exceptionContext, filterInfo.ServerExceptionFilters);

                if (!exceptionContext.ExceptionHandled)
                {
                    throw;
                }
            }
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_getFiltersThunk(job));
        }

        private static void PerformJobWithFilters(
            PerformContext context,
            IJobPerformer performer,
            IEnumerable<IServerFilter> filters)
        {
            var preContext = new PerformingContext(context);
            Func<PerformedContext> continuation = () =>
            {
                performer.Perform(context.Activator);
                return new PerformedContext(context, false, null);
            };

            var thunk = filters.Reverse().Aggregate(continuation,
                (next, filter) => () => InvokePerformFilter(filter, preContext, next));
            
            thunk();
        }

        private static PerformedContext InvokePerformFilter(
            IServerFilter filter, 
            PerformingContext preContext,
            Func<PerformedContext> continuation)
        {
            try
            {
                filter.OnPerforming(preContext);
            }
            catch (Exception filterException)
            {
                throw new JobPerformanceException(
                    "An exception occurred during execution of one of the filters",
                    filterException);
            }
            
            if (preContext.Canceled)
            {
                return new PerformedContext(
                    preContext, true, null);
            }

            var wasError = false;
            PerformedContext postContext;
            try
            {
                postContext = continuation();
            }
            catch (Exception ex)
            {
                wasError = true;
                postContext = new PerformedContext(
                    preContext, false, ex);

                try
                {
                    filter.OnPerformed(postContext);
                }
                catch (Exception filterException)
                {
                    throw new JobPerformanceException(
                        "An exception occurred during execution of one of the filters",
                        filterException);
                }

                if (!postContext.ExceptionHandled)
                {
                    throw;
                }
            }

            if (!wasError)
            {
                try
                {
                    filter.OnPerformed(postContext);
                }
                catch (Exception filterException)
                {
                    throw new JobPerformanceException(
                        "An exception occurred during execution of one of the filters",
                        filterException);
                }
            }

            return postContext;
        }

        private static void InvokeServerExceptionFilters(
            ServerExceptionContext context,
            IEnumerable<IServerExceptionFilter> filters)
        {
            foreach (var filter in filters.Reverse())
            {
                filter.OnServerException(context);
            }
        }
    }
}
