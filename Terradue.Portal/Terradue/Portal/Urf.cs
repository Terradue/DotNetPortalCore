using System;
using System.Runtime.Serialization;

namespace Terradue.Portal.Urf {

    [DataContract]
    public class Urf {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string TicketUrl { get; set; }
        [DataMember]
        public List<string> Links { get; set; }
        [DataMember]
        public UserInformation UserInformation { get; set; }
        [DataMember]
        public PortalInformation PortalInformation { get; set; }
        [DataMember]
        public UrfInformation UrfInformation { get; set; }
        [DataMember]
        public Requirements Requirements { get; set; }
    }

    [DataContract]
    public class UserInformation {
        [DataMember]
        public string Email { get; set; }
    }

    [DataContract]
    public class PortalInformation {
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Url { get; set; }
        [DataMember]
        public PortalPointOfContact PortalPointOfContact { get; set; }
    }

    [DataContract]
    public class PortalPointOfContact {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Url { get; set; }
    }

    [DataContract]
    public class UrfInformation {
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public string Version { get; set; }
        [DataMember]
        public UrfType Type { get; set; }
        [DataMember]
        public UrfStatus Status { get; set; }
        [DataMember]
        public string Feedback { get; set; }
        [DataMember]
        public string TotalPricing { get; set; }
        [DataMember]
        public string PurchaseAction { get; set; }
        [DataMember]
        public string ServiceType { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string ExpectedResults { get; set; }
        [DataMember]
        public DateTime UrfSubmissionDate { get; set; }
        [DataMember]
        public DateTime UrfStatusUpdateDate { get; set; }
        [DataMember]
        public DateTime ActivityStartDate { get; set; }
        [DataMember]
        public DateTime ActivityEndDate { get; set; }
        [DataMember]
        public UrfContact[] Contacts { get; set; }
    }

    [DataContract]
    public enum UrfType {
        DataExploitation,
        NewEOServiceIntegration,
        NewEoBasedProductGenerationAndReferencingAsInformationLayer
    }

    [DataContract]
    public enum UrfStatus {
        Saved,
        Submitted,
        UnderReview,
        UpdateRequested,
        Approved,
        Postponed,
        Activated,
        Rejected,
        Closed,
        Cancelled
    }

    [DataContract]
    public class UrfContact {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public bool Primary { get; set; }
        [DataMember]
        public string ContactOrganizationAcronym { get; set; }
        [DataMember]
        public string ContactOrganization { get; set; }
        [DataMember]
        public string ContactAddress { get; set; }
        [DataMember]
        public string ContactLastName { get; set; }
        [DataMember]
        public string ContactFirstName { get; set; }
        [DataMember]
        public string ContactNationality { get; set; }
        [DataMember]
        public string ContactTitle { get; set; }
        [DataMember]
        public string ContactPhone { get; set; }
        [DataMember]
        public string ContactFax { get; set; }
        [DataMember]
        public string ContactEmail { get; set; }
    }

    [DataContract]
    public class Requirements {
        [DataMember]
        public ScalerRequirements ScalerRequirements { get; set; }
        [DataMember]
        public ExplorerRequirements ExplorerRequirements { get; set; }
    }

    [DataContract]
    public class ScalerRequirements {
        [DataMember]
        public AOI[] Locations { get; set; }
        [DataMember]
        public DataCollection[] DataCollections { get; set; }
        [DataMember]
        public WpsService[] WpsServices { get; set; }
    }

    [DataContract]
    public class DataCollection {
        [DataMember]
        public string EoDataName { get; set; }
        [DataMember]
        public string EoDataInfo { get; set; }
    }

    [DataContract]
    public class AOI {
        [DataMember]
        public string AoIname { get; set; }
        [DataMember]
        public string AoIdesc { get; set; }
        [DataMember]
        public string AoIcoord { get; set; }
    }

    [DataContract]
    public class WpsService {
        [DataMember]
        public string WpsServiceName { get; set; }
        [DataMember]
        public string WpsServiceInfo { get; set; }
    }

    [DataContract]
    public class ExplorerRequirements {
        [DataMember]
        public ServiceIntegration Service { get; set; }
        [DataMember]
        public AutomaticProduction SystematicService { get; set; }
    }

    [DataContract]
    public class ServiceIntegration {
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Acronym { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public AOI[] Locations { get; set; }
        [DataMember]
        public DataCollection[] DataCollections { get; set; }
        [DataMember]
        public string BaseService { get; set; }
        [DataMember]
        public string BaseServiceComment { get; set; }
        [DataMember]
        public UrfDependency[] Dependencies { get; set; }
        [DataMember]
        public long ProcessingResourcesCpu { get; set; }
        [DataMember]
        public long ProcessingResourcesRam { get; set; }
        [DataMember]
        public long ProcessingResourcesFssize { get; set; }
        [DataMember]
        public string ProcessingResourcesComments { get; set; }
        [DataMember]
        public long ProcessingCloudCapacity { get; set; }
        [DataMember]
        public string ProcessingCloudCapability { get; set; }
    }

    [DataContract]
    public class AutomaticProduction {
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public DateTime StartDate { get; set; }
        [DataMember]
        public DateTime EndDate { get; set; }
        [DataMember]
        public AOI[] Locations { get; set; }
        [DataMember]
        public DataCollection[] DataCollections { get; set; }
        [DataMember]
        public string ExpectedDeliveryTime { get; set; }
        [DataMember]
        public string ExpectedProductionTime { get; set; }
        [DataMember]
        public string OutputContents { get; set; }
        [DataMember]
        public string OutputSpatialResolution { get; set; }
        [DataMember]
        public string OutputFileFormat { get; set; }
        [DataMember]
        public string OutputAuxiliaryFiles { get; set; }
        [DataMember]
        public string Protocols { get; set; }
        [DataMember]
        public string Comments { get; set; }
        [DataMember]
        public WpsService[] WpsServices { get; set; }
    }

    [DataContract]
    public class UrfDependency {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Url { get; set; }
        [DataMember]
        public string Info { get; set; }
    }

}
