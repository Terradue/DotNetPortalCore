using System;

/*!

\mainpage Terradue.Portal manual

\section intro_sec Introduction

Terradue.Portal along with its sibling projects Terradue.Portal.AdminTool and Terradue.Portal.Agent is the software at the backend of web portal for use in the area of geospatial data processing.

Its main software artefact is Terradue.Portal.dll which can be used in the .NET runtime environment and a related database scripts for making.

Terradue.Portal is required for all portals and can be extended by function-specific modules and site-specific modules. In a typical portal installation, there will be Terradue.Portal, several modules (e.g. for authentication functionality or connectors to cloud providers) and one site module (optional).

Terradue.Portal contains a number of classes. The two central classes are

<list type="bullet">
   <item><strong>IfyContext</strong>: This class contains core functionality for user session handling and database access. To process a request, usually from a web client, an instance is used for the entire process.</item>
   <item><strong>Entity</strong>: This class is the base class for all business objects, many of which are defined in Terradue.Portal itself. The Entity class provides functionality for loading and storing instances from and to the database.</item>
</list>

\section intro_sec_context The context

\section intro_sec_pers Entity persistence

As a general rule, Entity classes correspond to database tables and Entity instances correspond to records in those tables. This link is semi-automatic and realised by attributes on both classes and their properties.

The following concepts do not apply only to Terradue.Portal but to modules as well.

\subsection Definition of the database

The script db-create.sql contains the definition of the database that underlies Terradue.Portal. It is used by Terradue.Portal.AdminTool.exe, which installs and upgrades the database schema.

Terradue.Portal defines a number of basic entity types and

\subsection Definition of the classes

Classes

A new database schema version . Several changes can be

\subsection Changes to the model

There are several types of changes, the main changes are:

* Introduction of new entity types
* Modification to existing entity types
* Subclassing of existing entity types (see section Inheritance).

Several steps are necessary to do such a change:

* Create the new class or change the existing class.
* Change the main database creation script to included the new tables or the changes to existing tables.
* Add a new database script for an incremental upgrade from the previous version that results in the same schema as the one defined by the main database creation script.

\subsection intro_sec Inheritance

The database model allows to create subclasses of entity subclasses. When a new class is derived from an Entity subclass, that class usually contains new properties that need to be made persistent.

From the database modeling point of view, this should be done by adding an additional table, with a 1:1 relationship to the main table, in which the new fields are stored.

...


\defgroup Core Core
@{

The Core component is a set of library developed as the base implementation of a Content Management System (CMS) for EO world entities.
It implements basic subcomponent to deal with basic EO business objects such as dataset series, WPS service, user context, jobs...
It also implements the low level functions to store and read data persistently on the database or to apply a configuration.

@}

*/

/*!

\defgroup Security Security
@{

The Security component is a set of library in charge with all the authorisation or authentication functions and also with the privileges management 
between users, groups and other business objects. The security scheme is open and offers many possibilities to plug other component to
implement specific authentication mechanism or authorization scheme.

@}

*/

/*!
\defgroup Authorisation Authorisation
@{
It provides with the functions to define privileges for users or groups on \ref Entity objects for which restrictions are useful, such as entities that represent resources (\ref Service, \ref Series ...).

\ingroup Security

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence reads/writes the privileges persistently

\xrefitem dep "Dependencies" "Dependencies" uses \ref Context to identify the user and the session

The following class diagram describes the base authorization scheme implemented in the \ref Security component providing with the access control mechanism to the \ref Core and depending components.

\startuml Authorisation scheme Class Diagram

        Group  *-right-"0..*" User : has >
        User -down-"0..*" Domain : belongs >
        Group -down-"0..*" Domain : belongs >
        abstract class Entity
        Entity <|-up- Domain
        Entity <|-up- Object
        Entity -- EntityType : is specified by >
        EntityType -right- Privilege : defines >
        Role *-down- Privilege : is granted >
        class Permission
        User -- Object : accesses >
        Group -- Object : accesses >
        Object -right- Domain : belongs >

        (User, Domain) . Role
        (Group, Domain) . Role
        (User, Object) . Permission
        (Group, Object) . Permission

\enduml

The following defintions supports this scheme:
- \ref User and \ref Group are defined according the regular convention that a group is a set of zero or many users.
- A \ref Domain is an organizational unit to regroup \ref User, \ref Group and Objects (\ref Entity).
- A \ref Role defines the set of privileges that are granted to a \ref User or a \ref Group for a specific \ref Domain or globally.
- The assignement of a \ref Role to a \ref User or a \ref Group is called a "Role Grant". A Role Grant can be associated to a \ref Domain and is therefore called "Domain Role Grant".
A Role Grant not associated to any domain is a "Global Role Grant".
- A Privilege is an access control for a given entity (object) type. For all objects, there are 6 basic privileges : "Can Create", "Can Change", "Can Delete", "Can View", "Can Search" and "Can Manage".
For instance, "Can Create" for \ref Series specify the possibility to create a \Series in the system
- A Permission is a specific Privilege for a \ref User or a \ref Group for a given Object (\ref Entity). For instance: "Can View" for the ENVISAT \ref Series speificy the possibility to view the ENVISAT \ref Series in the results of a search.

And the following rules applies:
- Users and Groups with a Domain Role Grant on a certain \ref Domain have all the privileges defined by that Role on all Objects belonging to that \ref Domain.
- Users and Groups with a Global Role Grant have all the privileges defined by that Role on all Objects, whether belonging to a \ref Domain or not.
- A specific permission for a specific object is granted to a specific \ref User or \ref Group.


The authorisation consists of two phases:
- a generic phase where the current \ref User 's access privileges are compared to the necessary privileges for the accessed object according to the domain or the global.
- an optional specific phase where the same check is performed for the requested operation. This phase is specific to the \ref entity object in question as the possible operations are entity-specific.

The authorisation for a specific operation must be ensured by the code of the \ref Entity object. The central authorisation model supports this task by initialising the properties corresponding to the operation privilege that are applicable to the entity subclass.

\startuml Authorisation mechanism Activity Diagram

start
:Load entity item considering access policies and user/group privileges;
if (Are list/view privileges/permissions for current user sufficient?) then (yes)
    :Access granted;
else (no)
    if (Is current context set to restricted mode?) then (yes)
        :Access denied (throw exception);
        stop
    else (no)
        :Item flagged as unaccessible for current user (no exception);
    endif
    :Access granted;
endif
:Generic authorisation check completed;
:Speficic authorisation checks for operation (performed by entity subclass);
if (Is specific privilege or permission required for requested operation) then (yes)
    if (Does user have this privilege in the object's domain or this permission on the specific object?) then (no)
        :Operation rejected (throw exception);
        stop
    else (yes)
    endif
else (no)
endif
:Operation allowed;
stop

\enduml

@}
 */
namespace Terradue.Portal {

    public partial class Entity {}

}

