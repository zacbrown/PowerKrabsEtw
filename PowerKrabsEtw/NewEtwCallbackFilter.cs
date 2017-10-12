using O365.Security.ETW;

using System.Management.Automation;

namespace PowerKrabsEtw
{
    using Internal;

    [Cmdlet(VerbsCommon.New, "EtwCallbackFilter")]
    public class NewEtwCallbackFilter : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByProcessId")]
        public int ProcessId { get; set; } = -1;

        [Parameter(ParameterSetName = "ByEventId")]
        public int EventId { get; set; } = -1;

        [Parameter()]
        public SwitchParameter Not { get { return notFilter; } set { notFilter = value; } }
        private bool notFilter;

        /*
        [Parameter(ParameterSetName = "ByPropertyValue")]
        public string PropertyName { get; set; }

        [Parameter(ParameterSetName = "ByPropertyValue")]
        public string UnicodeStringContains { get; set; }

        [Parameter(ParameterSetName = "ByPropertyValue")]
        public string CountedUnicodeStringContains { get; set; }

        [Parameter(ParameterSetName = "ByPropertyValue")]
        public string AnsiStringContains { get; set; }

        [Parameter(ParameterSetName = "ByPropertyValue")]
        public string CountedAnsiStringContains { get; set; }
        */

        protected override void BeginProcessing()
        {
            // Check if they passed the right parameters.
            if (ProcessId == -1 && EventId == -1)
            {
                var error = new ErrorRecord(new PSArgumentException(),
                    nameof(PSArgumentException), ErrorCategory.InvalidArgument, this);
                WriteError(error);
                return;
            }

            var filter = GetConstructedFilter();
            var output = GetOutputObject();
            var filterProperty = new PSNoteProperty(nameof(Filter), filter);
            output.Properties.Add(filterProperty);

            WriteObject(filter);
        }

        private PSEtwFilter GetConstructedFilter()
        {
            Predicate eventIdPredicate = null;
            Predicate processIdPredicate = null;

            if (ProcessId != -1)
            {
                processIdPredicate = Filter.ProcessIdIs(ProcessId);
            }

            if (EventId != -1)
            {
                eventIdPredicate = Filter.EventIdIs(EventId);
            }

            Predicate finalPredicate = null;
            if (processIdPredicate != null && eventIdPredicate != null)
            {
                finalPredicate = processIdPredicate.And(eventIdPredicate);
            }
            else if (processIdPredicate != null)
            {
                finalPredicate = processIdPredicate;
            }
            else if (eventIdPredicate != null)
            {
                finalPredicate = eventIdPredicate;
            }

            if (notFilter) finalPredicate = Filter.Not(finalPredicate);
            return new PSEtwFilter(new EventFilter(finalPredicate));
        }

        private PSObject GetOutputObject()
        {
            var obj = new PSObject();

            if (ProcessId != -1)
            {
                obj.Properties.Add(new PSNoteProperty(nameof(ProcessId), ProcessId));
            }

            if (EventId != -1)
            {
                obj.Properties.Add(new PSNoteProperty(nameof(EventId), EventId));
            }

            if (notFilter)
            {
                obj.Properties.Add(new PSNoteProperty("IsNegated", notFilter));
            }

            return obj;
        }
    }
}
