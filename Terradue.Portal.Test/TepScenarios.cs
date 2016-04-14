using System;
using System.IO;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class TepScenarios {
        
        bool rebuildData = true;
        IfyContext context;

        Domain moveDomain, rldDomain;
        Role clusterProvisionRole, expertRole, contentAuthorityRole, softwareVendorRole, endUserRole, memberRole;
        Group moveGroup;
        User sarah, marco, jean, sofia, emma, dataProvider;
        Series demSeries, xstSeries;

        [TestFixtureSetUp]
        public void CreateEnvironment() {
        }

        [Test]
        public void TepTest() {
            string connectionString = BaseTest.GetConnectionString("scenariodb");
            if (rebuildData) {
                AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, Directory.GetCurrentDirectory() + "/../extended-db", null, connectionString);
                adminTool.Process();
            }
            context = new IfyLocalContext(connectionString, false);
            context.Open();

            try {
                if (rebuildData) {

                    context.AccessLevel = EntityAccessLevel.Administrator;

                    // Domains ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    moveDomain = new Domain(context);
                    moveDomain.Identifier = "MOVE";
                    moveDomain.Name = "MOVE domain";
                    moveDomain.Store();

                    rldDomain = new Domain(context);
                    rldDomain.Identifier = "RLD";
                    rldDomain.Name = "RLD domain";
                    rldDomain.Store();

                    // Users ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    sarah = new User(context);
                    sarah.Identifier = "sarah";
                    sarah.FirstName = "Sarah";
                    sarah.Store();

                    marco = new User(context);
                    marco.Identifier = "marco";
                    marco.FirstName = "Marco";
                    marco.LastName = "Rossi";
                    marco.Store();

                    jean = new User(context);
                    jean.Identifier = "jean";
                    jean.FirstName = "Jean";
                    jean.LastName = "Dupont";
                    jean.Store();

                    sofia = new User(context);
                    sofia.Identifier = "sofia";
                    sofia.FirstName = "Sofia";
                    sofia.LastName = "Rodriguez";
                    sofia.Store();

                    emma = new User(context);
                    emma.Identifier = "emma";
                    emma.FirstName = "Emma";
                    emma.LastName = "Muller";
                    emma.Store();

                    dataProvider = new User(context);
                    dataProvider.Identifier = "data-provider";
                    dataProvider.Store();

                    // Groups +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    moveGroup = new Group(context);
                    moveGroup.Identifier = "MOVE TEP group";
                    moveGroup.Store();
                    moveGroup.AssignUsers(new User[] {sarah, marco, jean, sofia, emma});

                    // Roles ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    clusterProvisionRole = new Role(context);
                    clusterProvisionRole.Identifier = "cluster-provision-role";
                    clusterProvisionRole.Name = "Cluster provision";
                    clusterProvisionRole.Store();
                    clusterProvisionRole.GrantToUser(sarah, moveDomain);

                    expertRole = new Role(context);
                    expertRole.Identifier = "expert-role";
                    expertRole.Name = "Expert";
                    expertRole.Store();
                    expertRole.GrantToUser(marco, moveDomain);

                    contentAuthorityRole = new Role(context);
                    contentAuthorityRole.Identifier = "content-authority-role";
                    contentAuthorityRole.Name = "Content authority";
                    contentAuthorityRole.Store();
                    contentAuthorityRole.GrantToUser(jean, moveDomain);

                    softwareVendorRole = new Role(context);
                    softwareVendorRole.Identifier = "software-vendor-role";
                    softwareVendorRole.Name = "Software vendor";
                    softwareVendorRole.Store();
                    softwareVendorRole.GrantToUser(sofia, moveDomain);

                    endUserRole = new Role(context);
                    endUserRole.Identifier = "end-user-role";
                    endUserRole.Name = "End user";
                    endUserRole.Store();
                    endUserRole.GrantToUser(emma, moveDomain);

                    memberRole = new Role(context);
                    memberRole.Identifier = "member-role";
                    memberRole.Name = "Member role";
                    memberRole.Store();
                    memberRole.GrantToUser(sarah, rldDomain);

                    // Data +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    demSeries = new Series(context);
                    demSeries.Identifier = "dem-series";
                    demSeries.Store();

                    xstSeries = new Series(context);
                    xstSeries.Identifier = "xst-series";
                    xstSeries.Store();


                } else {

                }
            } catch (Exception e) {
                Console.WriteLine("{0} - {1}", e.Message, e.StackTrace);
                throw;
            }












            try {
                context.ConsoleDebug = true;
                Terradue.Cloud.CloudProvider cloudProvider = new Terradue.Cloud.GenericCloudProvider(context);
                Console.WriteLine("CP: {0}", cloudProvider.EntityType.Id);
                cloudProvider.Identifier = "cloud-provider-1";
                cloudProvider.Name = "Cloud Provider 1";
                cloudProvider.Store();


                Terradue.Sandbox.Laboratory laboratory = Terradue.Sandbox.Laboratory.ForProvider(context, cloudProvider);

                laboratory.Identifier = "laboratory-1";
                laboratory.Name = "Laboratory 1";
                laboratory.Store();
            } catch (Exception e) {
                Console.WriteLine("{0} - {1}", e.Message, e.StackTrace);
                throw;
            }
                
        }

        [TestFixtureTearDown]
        public void DestroyEnvironment() {
            context.Close();
        }

    }










    namespace Terradue.Cloud {


        [EntityTable("cloudprov", EntityTableConfiguration.Custom, HasExtensions = true, NameField = "caption")]
        public abstract class CloudProvider : Entity {

            [EntityDataField("address")]
            public string AccessPoint { get; set; }

            [EntityDataField("web_admin_url")]
            public string WebAdminUrl { get; set; }

            public CloudProvider(IfyContext context) : base(context) {}

            public CloudProvider(IfyContext context, string accessPoint) : base(context) {
                this.AccessPoint = accessPoint;
            }

            public static new CloudProvider GetInstance(IfyContext context) {
                return new GenericCloudProvider(context);
            }

            public static CloudProvider FromId(IfyContext context, int id) {
                EntityType entityType = EntityType.GetEntityType(typeof(CloudProvider));
                CloudProvider result = (CloudProvider)entityType.GetEntityInstanceFromId(context, id); 
                result.Id = id;
                result.Load();
                return result;
            }

        }

        public class GenericCloudProvider : CloudProvider {

            //---------------------------------------------------------------------------------------------------------------------

            /// <summary>Creates a new GenericComputingResource instance.</summary>
            /// <param name="context">the execution environment context</param>
            public GenericCloudProvider(IfyContext context) : base(context) {}

        }


    }















    namespace Terradue.Sandbox {


        [EntityTable("laboratory", EntityTableConfiguration.Custom, NameField = "caption", HasPrivilegeManagement = true)]
        public class Laboratory : Entity {

            [EntityDataField("id_cloudprov", IsForeignKey = true)]
            public int ProviderId { get; protected set; }

            public Terradue.Cloud.CloudProvider Provider { get; protected set; }

            [EntityDataField("description")]
            public string Description { get; set; }

            [EntityPrivilegeField("is_manager")]
            public bool CanOperate { get; private set; }

            [EntityDataField("virtual_network")]
            public string VirtualNetworkName { get; set; }

            public Laboratory( IfyContext context ) : base( context ) {}

            public static new Laboratory GetInstance(IfyContext context) {
                return new Laboratory(context);
            }

            public static Laboratory ForProvider(IfyContext context, Terradue.Cloud.CloudProvider provider) {
                Laboratory result = new Laboratory(context);
                result.ProviderId = provider.Id;
                return result;
            }

            public static Laboratory FromId(IfyContext context, int id) {
                Laboratory result = new Laboratory(context);
                result.Id = id;
                result.Load();
                return result;
            }

        }

    
        public class Sandbox : Entity {

            //---------------------------------------------------------------------------------------------------------------------

            [EntityDataField("id_laboratory", IsForeignKey = true)]
            public int LaboratoryId { get; set; }

            //---------------------------------------------------------------------------------------------------------------------

            public Laboratory Laboratory { get; protected set; }

            //---------------------------------------------------------------------------------------------------------------------

            [EntityDataField("id_cloudcr", IsForeignKey = true)]
            public int ComputeResourceId { get; set; }

            //---------------------------------------------------------------------------------------------------------------------

            [EntityDataField("description")]
            public string Description { get; set; }

            //---------------------------------------------------------------------------------------------------------------------

            [EntityDataField("vm_template")]
            public string VirtualMachineTemplateName { get; protected set; }

            //---------------------------------------------------------------------------------------------------------------------

            [EntityDataField("virtual_disks")]
            public string VirtualDiskNames { get; protected set; }

            //---------------------------------------------------------------------------------------------------------------------

            public Terradue.Cloud.CloudProvider Provider {
                get { return (Laboratory == null ? null : Laboratory.Provider); }
            }

            //---------------------------------------------------------------------------------------------------------------------

            public Sandbox(IfyContext context) : base( context ) {
            }

            //---------------------------------------------------------------------------------------------------------------------

            public static new Sandbox GetInstance(IfyContext context) {
                return new Sandbox (context);
            }

            //---------------------------------------------------------------------------------------------------------------------

            public static Sandbox FromId(IfyContext context, int id) {
                EntityType entityType = EntityType.GetEntityType(typeof(Sandbox));
                Sandbox result = (Sandbox)entityType.GetEntityInstanceFromId(context, id);
                result.Id = id;
                result.Load();

                return result;
            }

            //---------------------------------------------------------------------------------------------------------------------

            public static Sandbox ForLaboratory(IfyContext context, int laboratoryId) {
                Sandbox result = new Sandbox (context);
                result.LaboratoryId = laboratoryId;
                return result;
            }

            //---------------------------------------------------------------------------------------------------------------------
/*
            /// Stop the sandbox. Target State = offline
            public void Stop(MachineStopMethod MachineStopMethod) {
            }

            //---------------------------------------------------------------------------------------------------------------------

            /// Suspend the sandbox. Target State = suspended
            public void Suspend(MachineSuspendMethod MachineSuspendMethod) {
                if (Appliance == null) return;

                Appliance.Suspend (MachineSuspendMethod.Suspend);
                context.AddInfo("Suspend command sent to Sandbox");

            }

            //---------------------------------------------------------------------------------------------------------------------

            /// Start the sandbox. Target State = online
            public void Resume() {
                if (Appliance == null) return;

                Appliance.Resume (MachineRestartMethod.Graceful);
                context.AddInfo("Resume command sent to Sandbox");
            }

            //---------------------------------------------------------------------------------------------------------------------

            /// Start the sandbox. Target State = online
            public void Shutdown() {
                if (Appliance == null) return;
                Appliance.Shutdown (MachineStopMethod.Graceful);
                context.AddInfo("Shutdown command sent to Sandbox");
            }

            //---------------------------------------------------------------------------------------------------------------------

            public bool SaveDiskAs(int id, string caption) {
                if (Appliance == null) return false; 
                bool result = Appliance.SaveDiskAs(id, caption);
                context.AddInfo("Disk save command sent to Sandbox");
                return result;
            }

            //---------------------------------------------------------------------------------------------------------------------

            public void TestOperation() {
                if (Appliance == null) return; 
                context.AddInfo("Test command sent to Sandbox");
                return; 
            }
*/
            //---------------------------------------------------------------------------------------------------------------------

            /// Backup the content of the Sandbox to a fresh copy. It returns the snapshot id
            public void Snapshot(int SandboxId) {
            }

            //---------------------------------------------------------------------------------------------------------------------

            /// Restore a previously snapshot of a sandbox
            public void Restore(int SandboxId) {
            }

            //---------------------------------------------------------------------------------------------------------------------

            public void ChangeState(string state) {
                switch (state) {
/*                    case "stop":
                        Stop (MachineStopMethod.Graceful);
                        break;
                    case "suspend":
                        Suspend (MachineSuspendMethod.Suspend);
                        break;
                    case "resume":
                        Resume ();
                        break;
                    case "shutdown":
                        Shutdown ();
                        break;
                    case "test":
                        TestOperation ();
                        break;
                    case "savediskas":
                        int diskid = int.Parse (HttpContext.Current.Request["disk_id"]);
                        string diskcaption = HttpContext.Current.Request["disk_caption"];
                        SaveDiskAs (diskid, diskcaption);
                        break;
                    case "recreate":

                    // if (DeleteAppliance()) {
                    //     context.AddInfo("The previous sandbox has been deleted");
                    //     return;
                    // } else {
                    //     webContext.ReturnHTTPError("The sandbox previous could not be deleted",400,"Bad Request");
                    // }

                    // Create the clone appliance that will run the new sandbox
                    CloudAppliance ApplianceInit = Provider.CreateInstance(Name, VirtualMachineTemplateName, VirtualDiskNames.Split ('\t'), Laboratory.VirtualNetworkName);

                    // Create the CloudComputingResource of the sandbox
                    // A Sandbox is also a potential CloudComputingResource. Today, we assume that it is always an Oozie type
                    OozieComputingResource cr = OozieComputingResource.OnAppliance (context, ApplianceInit, false);
                    cr.Store ();
                    ComputeResourceId = cr.Id;

                    // Finally store the Sandbox
                    Store ();
                    break;*/

                }
            }

            //---------------------------------------------------------------------------------------------------------------------

            public bool DeleteAppliance() {
                return false;
            } 

        }

    }




}

