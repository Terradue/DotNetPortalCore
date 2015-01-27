using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Collections.Specialized;





namespace Terradue.Portal {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// <summary>
    /// I scheduler run configuration.
    /// </summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public interface ISchedulerRunConfiguration {
        void Reset();
        NameValueCollection GetNextParameters();
        void SetParameters(NameValueCollection parameters);
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Abstract class representing a collection of scheduler parameters and settings for generating subsequent runs of the scheduler.</summary>
    /// <remarks>All schedulers require a run configuration to manage the advancing of its parameters.</remarks>
    /// \xrefitem uml "UML" "UML Diagram"
    [EntityTable("schedulerrunconf", EntityTableConfiguration.Custom, HasAutomaticIds = false, HasExtensions = true)]
    public abstract class SchedulerRunConfiguration : Entity, ISchedulerRunConfiguration {

        private Scheduler scheduler;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the scheduler.
        /// </summary>
        /// <value>The scheduler.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public Scheduler Scheduler {
            get {
                if (scheduler == null && Id != 0) {
                    scheduler = Scheduler.FromId(context, Id);
                    scheduler.RunConfiguration = this;
                }
                return scheduler;
            }
            set {
                scheduler = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual string ModeCaption { 
            get { return "---"; }
        }

        public SchedulerRunConfiguration(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new SchedulerRunConfiguration instance representing the scheduler run configuration with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The scheduler run configuration's database ID.</param>
        /// <returns>The created SchedulerRunConfiguration object.</returns>
        public static SchedulerRunConfiguration FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(SchedulerRunConfiguration));
            SchedulerRunConfiguration result = (SchedulerRunConfiguration)entityType.GetEntityInstanceFromId(context, id);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Load() {
            base.Load();
            //Parameters.Load();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            if (Id == 0) Id = Scheduler.Id;
            base.Store();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool IsCompleted() {
            return Scheduler.Status == SchedulingStatus.Completed;
        }

        //---------------------------------------------------------------------------------------------------------------------

        #region ISchedulerRunConfiguration

        public abstract void Reset();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, gets the next parameters according to the run configuration rules.</summary>
        /// <returns>A NameValueCollection containing the name/value pairs for the next parameters or <c>null</c> if there are no next parameters.</returns>
        public abstract NameValueCollection GetNextParameters();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, sets the parameters to the specified values specified according to the run configuration rules.</summary>
        /// <param name="parameters">A NameValueCollection containing the name/value pairs for the parameters.</returns>
        public abstract void SetParameters(NameValueCollection parameters);

        //---------------------------------------------------------------------------------------------------------------------

        #endregion

        public abstract string GetExecutionLogMessage();

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents a collection of parameters and settings for generating subsequent runs of the scheduler based on temporal selection parameters.</summary>
    [EntityTable("timeschedulerrunconf", EntityTableConfiguration.Custom)]
    public class TimeDrivenRunConfiguration : SchedulerRunConfiguration {

        private string timeInterval;

        //---------------------------------------------------------------------------------------------------------------------

        protected override string ModeCaption {
            get { return "time-driven"; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the start time of the valdity period.</summary>
        [EntityDataField("validity_start")]
        public DateTime ValidityStart { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the end time of the valdity period.</summary>
        [EntityDataField("validity_end")]
        public DateTime ValidityEnd { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the time interval value.</summary>
        [EntityDataField("time_interval")]
        public string TimeInterval { 
            get { return timeInterval; }
            set { 
                timeInterval = value;
                TimeIntervalSeconds = StringUtils.StringToSeconds(timeInterval);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the selection period is shifted by the time interval value with each run.</summary>
        /// <remarks>.</remarks>
        [EntityDataField("shifting")]
        public bool PeriodShifting { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the scheduler execution time has to be always in the past.</summary>
        [EntityDataField("past_only")]
        public bool PastOnly { get; set; } // !!! make changeable in 

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the latest processed time between the validity start and end times.</summary>
        [EntityDataField("ref_time")]
        public DateTime ReferenceTime { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the time interval value.</summary>
        public int TimeIntervalSeconds { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public TimeDrivenRunConfiguration(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public override void Reset() {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override NameValueCollection GetNextParameters() {
            NameValueCollection result = new NameValueCollection();
            // Get OpenSearch parameters from OpenSearch query

            DateTime nextReferenceTime;
            if (Scheduler.Status == SchedulingStatus.Ready) {
                ReferenceTime = ValidityStart;
            } else if (Scheduler.Status == SchedulingStatus.Ready) {
                nextReferenceTime = ReferenceTime;
                StringUtils.CalculateDateTime(ref nextReferenceTime, TimeInterval, false);
                if ((nextReferenceTime - ValidityEnd).TotalSeconds * (TimeIntervalSeconds < 0 ? -1 : 1) > 0) {
                    Scheduler.Status = SchedulingStatus.Completed;
                    return null;
                }
                ReferenceTime = nextReferenceTime;
            }

            foreach (ProcessingInputSet inputSet in Scheduler.Inputs) {
                foreach (ServiceParameter param in inputSet.SearchParameters) {
                    switch (param.SearchExtension) {
                        case "time:start" :
                            DateTime newStartDate = ReferenceTime;
                            if (param.Value != null && param.Value.Contains("$(EXECDATE)")) {
                                StringUtils.CalculateDateTime(ref newStartDate, param.Value.Replace("$(EXECDATE)", String.Empty), false);
                            }
                            result.Add(param.Name, newStartDate.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z"));
                            break;
                        case "time:end" :
                            DateTime newEndDate = ReferenceTime;
                            if (param.Value != null && param.Value.Contains("$(EXECDATE)")) {
                                StringUtils.CalculateDateTime(ref newEndDate, param.Value.Replace("$(EXECDATE)", String.Empty), false);
                            }
                            result.Add(param.Name, newEndDate.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z"));
                            break;
                    }
                }
            }

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void SetParameters(NameValueCollection parameters) {
            if (Scheduler.Status == SchedulingStatus.Ready) {
                ReferenceTime = ValidityStart;
                Scheduler.Status = SchedulingStatus.Running;
            } else if (Scheduler.Status == SchedulingStatus.Running) {
                DateTime nextReferenceTime = ReferenceTime;
                StringUtils.CalculateDateTime(ref nextReferenceTime, TimeInterval, false);

                // If the reference time is in the future, do nothing.
                if (PastOnly && (nextReferenceTime - context.Now).TotalSeconds * (TimeIntervalSeconds < 0 ? -1 : 1) > 0) return;

                // If the reference time is beyond the validity time, set the status to completed.
                if ((nextReferenceTime - ValidityEnd).TotalSeconds * (TimeIntervalSeconds < 0 ? -1 : 1) > 0) {
                    Scheduler.Status = SchedulingStatus.Completed;
                } else {
                    ReferenceTime = nextReferenceTime;
                    Scheduler.Status = SchedulingStatus.Running;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void SetNextTaskParameters(RequestParameterCollection requestParameters) {

            // Set new parameters
            /*RequestParameter param;
            for (int i = 0; i < requestParameters.Count; i++) {
                param = requestParameters[i]; 
                switch (param.Type) {
                    case "startdate" :
                        case "enddate" :
                        case "date" :
                        case "datetime" :
                        if (param.Value != null && param.Value.Contains("$(EXECDATE)")) {
                            DateTime dt = NextReferenceTime;
                            StringUtils.CalculateDateTime(ref dt, param.Value.Replace("$(EXECDATE)", String.Empty), false);
                            param.Value = dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss");
                        }
                        break;
                        case "caption" :
                        if (param.Value == null) break;
                        param.Value = param.Value.Replace("$(EXECDATE)", ReferenceTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"));
                        break;
                }
            }*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override string GetExecutionLogMessage() {
            return String.Format("reference time of last run: {0}", ReferenceTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss.fff\Z"));        
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents a collection of parameters and settings for generating subsequent runs of the scheduler based on data availability.</summary>
    [EntityTable("dataschedulerrunconf", EntityTableConfiguration.Custom)]
    public class DataDrivenRunConfiguration : SchedulerRunConfiguration {

        //---------------------------------------------------------------------------------------------------------------------

        protected override string ModeCaption {
            get { return "data-driven"; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the start time of the valdity period.</summary>
        [EntityDataField("validity_start")]
        public DateTime ValidityStart { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the end time of the valdity period.</summary>
        [EntityDataField("validity_end")]
        public DateTime ValidityEnd { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the scheduler execution time moves backward when the scheduler advances.</summary>
        [EntityDataField("back_dir")]
        public bool BackwardDirection { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the minimum number of files necessary to create a new scheduler task.</summary>
        [EntityDataField("min_files")]
        public int MinFiles { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the maximum number of files that can be processed by a single scheduler task.</summary>
        [EntityDataField("max_files")]
        public int MaxFiles { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the latest processed time between the validity start and end times.</summary>
        [EntityDataField("last_time")]
        public DateTime LastProductTime { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the milliseconds part pf the the latest processed time.</summary>
        [EntityDataField("last_time_ms")]
        public int LastProductTimeMilliseconds { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the identifier of the last file processed within the latest task.</summary>
        [EntityDataField("last_identifier")]
        public string LastProductIdentifier { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the filtering operations are done after the input product query.</summary>
        /// <remarks>
        ///     Filtering matching products with the OpenSearch query is a more efficient way of data-driven scheduling.
        ///     If the OpenSearch catalogue supports filtering and sorting by modification time and product identifiers, the value <c>false</c> is the better choice.
        /// </remarks>
        public bool FilterAfterQuery {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected bool HasValidity {
            get { return ValidityStart != DateTime.MinValue && ValidityEnd != DateTime.MinValue; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool EndReached {
            get {
                double beyondEnd = (LastProductTime - ValidityEnd).TotalSeconds * (BackwardDirection ? -1 : 1);
                return (beyondEnd > 0);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public DataDrivenRunConfiguration(IfyContext context) : base(context) {
            this.MinFiles = 1;
            this.MaxFiles = 1;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Load() {
            base.Load();
            LastProductTime.AddMilliseconds(LastProductTimeMilliseconds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            LastProductTimeMilliseconds = LastProductTime.Millisecond;
            base.Store();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Reset() {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override NameValueCollection GetNextParameters() {
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void SetParameters(NameValueCollection parameters) {
            if (Scheduler.Status == SchedulingStatus.Ready) Scheduler.Status = SchedulingStatus.Running;
            if (Scheduler.Status == SchedulingStatus.Running) {
                /*DateTime nextReferenceTime = LastProductTime;
                StringUtils.CalculateDateTime(ref nextReferenceTime, TimeInterval, false);

                // If the reference time is in the future, do nothing.
                if (PastOnly && (nextReferenceTime - context.Now).TotalSeconds * (TimeIntervalSeconds < 0 ? -1 : 1) > 0) return;

                // If the reference time is beyond the validity time, set the status to completed.
                if ((nextReferenceTime - ValidityEnd).TotalSeconds * (TimeIntervalSeconds < 0 ? -1 : 1) > 0) {
                    Scheduler.Status = SchedulingStatus.Completed;
                } else {
                    ReferenceTime = nextReferenceTime;
                    Scheduler.Status = SchedulingStatus.Running;
                }*/
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override string GetExecutionLogMessage() {
            return String.Format("last input product modification time: {0}, last product name: {1}", LastProductTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss.fff\Z"), LastProductIdentifier);        
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool GetNextInputData(Task task) { // TODO-NEW-SERVICE

            try {
                /*ServiceDerivate derivate = task; // the derivate will be either the task (if given) or the scheduler itself 
            
                if (Seriesx.Count == 0) {
                    Service.GetParameters();
                    RequestParameters.ResolveOwnerships();
                
                    IDataReader reader = context.GetQueryResult("SELECT name, type, value FROM schedulerparam WHERE id_scheduler=" + Id + ";");
                    while (reader.Read()) {
                        RequestParameters.GetParameter(context, Service, reader.GetString(0), reader.GetString(1), null, reader.GetString(2));
                        //param.Level = RequestParameterLevel.Custom;
                    }
                    reader.Close();
        
                    for (int i = 0; i < RequestParameters.Count; i++) {
                        RequestParameter param = RequestParameters[i];
                        
                        switch (param.Source) {
                            case "series" :
                            case "Task.Series" :
                                Series series = null;
                                
                                ServiceParameterValueSet optionSource = param.SpecificValueSet as ServiceParameterValueSet;
                                if (optionSource != null) {
                                    XmlElement seriesOption = optionSource.SelectedElement;
                                    if (!seriesOption.HasAttribute("value") || seriesOption.Attributes["value"].Value != param.Value) continue;
                                    
                                    string catalogueDescriptionUrl = (seriesOption.HasAttribute("description") ? seriesOption.Attributes["description"].Value : null);
                                    string catalogueUrlTemplate = (seriesOption.HasAttribute("template") ? seriesOption.Attributes["template"].Value : null);
                                    if (catalogueDescriptionUrl != null) series = new CustomSeries(context, param.Value, null, catalogueDescriptionUrl, catalogueUrlTemplate);
                                }
                    
                                if (series == null) {
                                    context.HideMessages = true;
                                    try {
                                        series = Series.FromString(context, param.Value);
                                    } catch (EntityNotFoundException) {}
                                    context.HideMessages = false;
                                }
                                
                                if (series != null) Seriesx.Add(param.Name, series);
        
                                break;
                        }
                    }
                }
                
                if (derivate == null) derivate = this;
                
                // Below the catalogue query is executed
                
                if (derivate.Seriesx.Count == 0) {
                    Invalidate("Missing series information");
                    return false;
                }

                int count = 0;

                for (int i = 0; i < derivate.Seriesx.Count; i++) {
                    //context.AddInfo(Seriesx[i].Name);
                    //for (int i = 0; i < RequestParameters.Count; i++) context.AddInfo("RP " + RequestParameters[i].Name + " = " + RequestParameters[i].Value + ", " + RequestParameters[i].SearchExtension);
                    RequestParameter param;
                    param = new RequestParameter(context, Service, "_sort", null, null, String.Format("dct.modified,,{0} dc.identifier,,{0}", BackwardDirection ? "descending" : ""));
                    param.SearchExtension = "sru:sortKeys";
                    param.Optional = true;
                    derivate.RequestParameters.Add(param);
                    
                    //context.AddDebug(1, ReferenceTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff") + " " + ValidityStart.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff") + " " + ReferenceTime.Millisecond);
                    
                    string limitValue = (Started ? ReferenceTime : ValidityStart).ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff\Z");
                    param = new RequestParameter(context, Service, "_modified", null, null, "::" + limitValue);
                    param.SearchExtension = "dct:modified";
                    param.Optional = true;
                    derivate.RequestParameters.Add(param);
        
                    if (Started && ReferenceFile != null) {
                        param = new RequestParameter(context, Service, "_lastFile", null, null, ":" + ReferenceFile);
                        param.SearchExtension = "geo:uid";
                        derivate.RequestParameters.Add(param);
                    }
                    
                    derivate.MatchInputRelatedParameters();
                    
                    derivate.Seriesx[i].CatalogueResult.AddMetadataField("_modified", "dct:modified"); // !!! maybe better a dedicated field DataSetInformation.ModificationTime
                    derivate.Seriesx[i].CatalogueResult.AddMetadataField("_identifier", "dc:identifier"); // !!! maybe better a dedicated field DataSetInformation.ModificationTime
                    
                    //context.AddInfo(derivate.Seriesx[i].CatalogueUrlTemplate);
                    //context.AddInfo(derivate.Seriesx[i].CatalogueResult.Url);
                    //for (int j = 0; j < RequestParameters.Count; j++) context.AddInfo(RequestParameters[j].Name + " " + RequestParameters[j].SearchExtension);
                    
                    if (derivate.Seriesx[i].CatalogueResult != null) { 
                        if (task == null) {
                            Seriesx[i].CatalogueResult.GetTotalResults();
                            count += Seriesx[i].CatalogueResult.TotalResults;
                        } else {
                            task.Seriesx[i].CatalogueResult.GetDataSets(MinFiles, MaxFiles);
                            count += task.Seriesx[i].CatalogueResult.ReceivedResults;
                        }
                    }
                    
                    //context.AddInfo("TotalResults = " + derivate.Seriesx[i].CatalogueResult.TotalResults);
                    //for (int j = 0; j < derivate.Seriesx[i].CatalogueResult.ReceivedResults; j++) {
                    //    context.AddInfo(derivate.Seriesx[i].CatalogueResult.DataSets[j].Name + " " + derivate.Seriesx[i].CatalogueResult.DataSets[j]["dtstart"] + " " + derivate.Seriesx[i].CatalogueResult.DataSets[j]["dtend"] + " ----- " + derivate.Seriesx[i].CatalogueResult.DataSets[j]["_modified"]);
                    //}
                }
                
                //if (task == null) return false;
                    
                if (count < MinFiles) {
                    derivate.Invalidate("Cannot create a new task now, the minimum number of new input files has not been reached");
                    return false;
                    //errorAdded = true;
                }
                
                // If a task creation is requested, add the r
                if (task != null) {
                    DateTime referenceTime;
                    string referenceTimeStr = null, referenceFile = null;
                    count = 0;
                    for (int i = 0; i < task.Seriesx.Count && count < MaxFiles; i++) {
                        string value = null;
                        //context.AddInfo("XXX " + task.Seriesx[i].CatalogueResult.ReceivedResults);
                        for (int j = 0; j < task.Seriesx[i].CatalogueResult.ReceivedResults && count < MaxFiles; j++) {
                            referenceTimeStr = task.Seriesx[i].CatalogueResult.DataSets[j]["_modified"].AsString;
                            referenceFile = task.Seriesx[i].CatalogueResult.DataSets[j].Identifier;
                            //context.AddInfo("FILE: " + referenceTimeStr + " * " + referenceFile);
                            if (j == 0) value = String.Empty; else value += ",";
                            value += task.Seriesx[i].CatalogueResult.DataSets[j].Resource;
                            count++;
                        }
                        RequestParameter param = new RequestParameter(context, Service, task.Seriesx[i].DataSetParameter.Name, null, null, value);
                        //param.Level = RequestParameterLevel.Custom;
                        task.RequestParameters.Add(param);
                    }
                    if (referenceTimeStr != null && DateTime.TryParse(referenceTimeStr, out referenceTime)) ReferenceTime = referenceTime.ToUniversalTime();
                    ReferenceFile = referenceFile;
                }
                */
                return true; // done

            } catch (Exception e) { 
                context.AddError(e.Message); return false;
            }
        }

    }
}

