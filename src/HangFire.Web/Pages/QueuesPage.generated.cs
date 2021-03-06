﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HangFire.Web.Pages
{
    
    #line 2 "..\..\Pages\QueuesPage.cshtml"
    using System;
    
    #line default
    #line hidden
    using System.Collections.Generic;
    
    #line 3 "..\..\Pages\QueuesPage.cshtml"
    using System.Linq;
    
    #line default
    #line hidden
    using System.Text;
    
    #line 4 "..\..\Pages\QueuesPage.cshtml"
    using Common;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Pages\QueuesPage.cshtml"
    using HangFire.Storage;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Pages\QueuesPage.cshtml"
    using Pages;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Pages\QueuesPage.cshtml"
    using Storage.Monitoring;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class QueuesPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");









            
            #line 9 "..\..\Pages\QueuesPage.cshtml"
  
    Layout = new LayoutPage { Title = "Queues" };

    IList<QueueWithTopEnqueuedJobsDto> queues;

    using (var monitor = JobStorage.Current.GetMonitoringApi())
    {
        queues = monitor.Queues();
    }


            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 20 "..\..\Pages\QueuesPage.cshtml"
 if (queues.Count == 0)
{

            
            #line default
            #line hidden
WriteLiteral("    <div class=\"alert alert-warning\">\r\n        No queued jobs found. Try to enque" +
"ue a job.\r\n    </div>\r\n");


            
            #line 25 "..\..\Pages\QueuesPage.cshtml"
}
else
{

            
            #line default
            #line hidden
WriteLiteral(@"    <table class=""table table-striped"">
        <thead>
            <tr>
                <th>Queue</th>
                <th>Length</th>
                <th>Fetched</th>
                <th>Next jobs</th>
            </tr>
        </thead>
        <tbody>
");


            
            #line 38 "..\..\Pages\QueuesPage.cshtml"
             foreach (var queue in queues)
            {

            
            #line default
            #line hidden
WriteLiteral("                <tr>\r\n                    <td>\r\n                        <a class=" +
"\"label-queue\" href=\"");


            
            #line 42 "..\..\Pages\QueuesPage.cshtml"
                                                Write(Request.LinkTo("/queues/" + queue.Name));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                            ");


            
            #line 43 "..\..\Pages\QueuesPage.cshtml"
                       Write(queue.Name);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </a>\r\n                    </td>\r\n                    <t" +
"d>");


            
            #line 46 "..\..\Pages\QueuesPage.cshtml"
                   Write(queue.Length);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                    <td>\r\n                        <a href=\"");


            
            #line 48 "..\..\Pages\QueuesPage.cshtml"
                            Write(Request.LinkTo("/queues/fetched/" + queue.Name));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                            ");


            
            #line 49 "..\..\Pages\QueuesPage.cshtml"
                       Write(queue.Fetched);

            
            #line default
            #line hidden

            
            #line 49 "..\..\Pages\QueuesPage.cshtml"
                                          WriteLiteral("    \r\n                        </a>\r\n                    </td>\r\n                  " +
"  <td>");

            
            #line default
            #line hidden
            
            #line 52 "..\..\Pages\QueuesPage.cshtml"
                         if (queue.FirstJobs.Count == 0)
                        {

            
            #line default
            #line hidden
WriteLiteral("                        <em>No jobs queued.</em>\r\n");


            
            #line 55 "..\..\Pages\QueuesPage.cshtml"
                        }
                        else
                        {

            
            #line default
            #line hidden
WriteLiteral(@"                        <table class=""table table-condensed table-bordered table-inner"">
                            <thead>
                                <tr>
                                    <th>Id</th>
                                    <th>Job</th>
                                    <th>Enqueued</th>
                                </tr>
                            </thead>
                            <tbody>
");


            
            #line 67 "..\..\Pages\QueuesPage.cshtml"
                                 foreach (var job in queue.FirstJobs)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <tr class=\"");


            
            #line 69 "..\..\Pages\QueuesPage.cshtml"
                                           Write(!job.Value.InEnqueuedState ? "obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                        <td>\r\n                               " +
"             <a href=\"");


            
            #line 71 "..\..\Pages\QueuesPage.cshtml"
                                                Write(Request.LinkTo("/job/" + job.Key));

            
            #line default
            #line hidden
WriteLiteral("\">");


            
            #line 71 "..\..\Pages\QueuesPage.cshtml"
                                                                                    Write(HtmlHelper.JobId(job.Key));

            
            #line default
            #line hidden
WriteLiteral("</a>\r\n");


            
            #line 72 "..\..\Pages\QueuesPage.cshtml"
                                             if (!job.Value.InEnqueuedState)
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <span title=\"Job\'s state has been" +
" changed while fetching data.\" class=\"glyphicon glyphicon-question-sign\"></span>" +
"\r\n");


            
            #line 75 "..\..\Pages\QueuesPage.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n                                  " +
"      <td>\r\n                                            <span title=\"");


            
            #line 78 "..\..\Pages\QueuesPage.cshtml"
                                                    Write(HtmlHelper.DisplayMethodHint(job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                                ");


            
            #line 79 "..\..\Pages\QueuesPage.cshtml"
                                           Write(HtmlHelper.DisplayMethod(job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                            </span>\r\n                          " +
"              </td>\r\n                                        <td>\r\n");


            
            #line 83 "..\..\Pages\QueuesPage.cshtml"
                                             if (job.Value.EnqueuedAt.HasValue)
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <span data-moment=\"");


            
            #line 85 "..\..\Pages\QueuesPage.cshtml"
                                                              Write(JobHelper.ToStringTimestamp(job.Value.EnqueuedAt.Value));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                                    ");


            
            #line 86 "..\..\Pages\QueuesPage.cshtml"
                                               Write(job.Value.EnqueuedAt);

            
            #line default
            #line hidden
WriteLiteral("\r\n                                                </span>\r\n");


            
            #line 88 "..\..\Pages\QueuesPage.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n                                  " +
"  </tr>\r\n");


            
            #line 91 "..\..\Pages\QueuesPage.cshtml"
                                }

            
            #line default
            #line hidden
WriteLiteral("                            </tbody>\r\n                        </table>\r\n");


            
            #line 94 "..\..\Pages\QueuesPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </td>\r\n                </tr>\r\n");


            
            #line 97 "..\..\Pages\QueuesPage.cshtml"
            }

            
            #line default
            #line hidden
WriteLiteral("        </tbody>\r\n    </table>\r\n");


            
            #line 100 "..\..\Pages\QueuesPage.cshtml"
}
            
            #line default
            #line hidden

        }
    }
}
#pragma warning restore 1591
