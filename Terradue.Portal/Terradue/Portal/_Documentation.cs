using System;

/*!

\mainpage Terradue.Portal manual

\section sec_intro Introduction

Terradue.Portal along with its sibling projects Terradue.Portal.AdminTool and Terradue.Portal.Agent is the software at the backend of web portal for use in the area of geospatial data processing.

Its main software artefact is Terradue.Portal.dll which can be used in the .NET runtime environment and a related database scripts for making.

Terradue.Portal is required for all portals and can be extended by function-specific modules and site-specific modules. In a typical portal installation, there will be Terradue.Portal, several modules (e.g. for authentication functionality or connectors to cloud providers) and one site module (optional).

Terradue.Portal contains a number of classes. The two central classes are

<list type="bullet">
   <item><strong>IfyContext</strong>: This class contains core functionality for user session handling and database access. To process a request, usually from a web client, an instance is used for the entire process.</item>
   <item><strong>Entity</strong>: This class is the base class for all business objects, many of which are defined in Terradue.Portal itself. The Entity class provides functionality for loading and storing instances from and to the database.</item>
</list>

\section sec_context The context

\section sec_entitypers Entity persistence

<para>As a general rule, Entity classes correspond to database tables and Entity instances correspond to records in those tables. This link is semi-automatic and realised by attributes on both classes and their properties.</para>

<para>Entity items must have a <strong>unique numeric ID</strong> (this corresponds to the primary key in the database) and can have a <strong>unique text identifier</strong> and a <strong>human-readable name</strong>.</para>

<para>In the following sections, the concept is illustrated by simple examples. This does not apply only to Terradue.Portal but to modules as well.</para>

\subsection sec_dbdef Database definition

The script db-create.sql contains the definition of the database that underlies Terradue.Portal. It is used by Terradue.Portal.AdminTool.exe, which installs and upgrades the database schema.

Terradue.Portal defines a number of basic entity types.

A simple type could be represented in this table:

<code>
CREATE TABLE rect (
    id int unsigned NOT NULL auto_increment,
    width int unsigned NOT NULL COMMENT 'Width',
    height int unsigned NOT NULL COMMENT 'Height',
    CONSTRAINT pk_rect PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Rectangles';
</code>

\subsection sec_classdef Class definition

A new database schema version . Several changes can be

For the above example, the class would look like this:

<code>
    [EntityTable("rect", EntityTableConfiguration.Custom)]
    public class Rect : Entity {

        [EntityDataField("width")]
        public int Width { get; set; }

        [EntityDataField("height")]
        public int Height { get; set; }

        // ...

    }
</code>

\subsection sec_changes Changes to the model

There are several types of changes, the main changes, which are explained in more details in the following sections, are:

<list type="bullet">
    <item>Introduction of new entity types,</item>
    <item>Modification to existing entity types, and</item>
    <item>Subclassing of existing entity types (see section Inheritance).</item>
</list>

Several steps are necessary to do such a change (unlike above, the order is code-first here for more intuitive understanding):

<list type="bullet">
    <item><strong>(1)</strong> Create the new class or change the existing class.</item>
    <item><strong>(2)</strong> Change the main database creation script to included the new tables or the changes to existing tables. Increment the schema's version number.</item>
    <item><strong>(3)</strong> Add a new database script for an incremental upgrade from the previous version that results in the same schema as the one defined by the main database creation script.</item>
</list>

What these steps mean for each of the change types is explained in the following sections.

\subsection sec_changes_1 Introduction of new entity types

<para><strong>(1)</strong> Create the new class, in the same way as the sample class above.</para>
<para><strong>(2)</strong> Add the corresponding table, defined in the same way as the sample table above, to the database creation script.</para>
<para><strong>(3)</strong> Create a new database upgrade script that contains the same <c>CREATE TABLE</c> definition that was added to the database creation script.</para>

\subsection sec_changes_2 Modification to existing entity types

<para>Before modifying an existing entity, consider whether it might be better to create subclasses (see next section).</para>

<para><strong>(1)</strong> Modify the existing class. For example, add a new property.</para>
<code>
    [EntityTable("rect", EntityTableConfiguration.Custom)]
    public class Rect : Entity {

        [EntityDataField("width")]
        public int Width { get; set; }

        [EntityDataField("height")]
        public int Height { get; set; }

        // NEW property: <==========
        [EntityDataField("color")]
        public int Color { get; set; }

        // ...

    }
</code>

<para><strong>(2)</strong> Modify the corresponding table in the database creation script so that it reflects the changes made to the class.</para>
<code>
CREATE TABLE rect (
    id int unsigned NOT NULL auto_increment,
    width int unsigned NOT NULL COMMENT 'Width',
    height int unsigned NOT NULL COMMENT 'Height',
    color int unsigned COMMENT 'Color', -- new column <==========
    CONSTRAINT pk_rect PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Rectangles';
</code>

<para><strong>(3)</strong> Create a new database upgrade script that contains a modification command that alters the existing table so that it is in line with the one now defined in the database creation script.</para>
<code>
-- Changing structure of table "rect" (adding column "color") ...\
ALTER TABLE rect
    ADD COLUMN color int unsigned COMMENT 'Color'
;
-- RESULT
</code>
<para>Note: the comments before and after the command are used by Terradue.Admin.Tool that will runs this script for a user-friendly output.

\subsection sec_changes_3 Subclassing of existing entity types

<para>Before creating subclasses, consider whether the changes could also be integrated in the base class (see previous section). Subclassing makes most sense if there are several different subclasses.</para>

<para><strong>(1)</strong> Since there is now a subclass to the base class, the base class needs to be marked as an extensible class.</para>
<code>
    [EntityTable("rect", EntityTableConfiguration.Custom, HasExtensions = true)]
    public class ColoredRect : Rect {

        // ...

    }
</code>

<para>Create a new class based on the existing class and add a new property.</para>
<code>
    [EntityTable("coloredrect", EntityTableConfiguration.Custom)]
    public class ColoredRect : Rect {

        [EntityDataField("color")]
        public int Color { get; set; }

        // ...

    }
</code>
<para><strong>(2)</strong> Since there is now a subclass, type references are needed.<br/>In the database creation script, add both the base class and the subclass (extended class) as new types (via stored procedure <c>add_type</c>, which fills the table <em>type</em>) in the appropriate section of the script. Make sure the fully qualified names are correct.<br/>Then add a type reference in the base table.</para>
<code>
CALL add_type(NULL, 'Terradue.Portal.Rect, Terradue.Portal', NULL, 'Rectangle', 'Rectangles', 'rectangles');
CALL add_type(NULL, 'Terradue.Portal.ColoredRect, Terradue.Portal', 'Terradue.Portal.Rect, Terradue.Portal', 'Colored rectangle', 'Colored rectangles', NULL);

CREATE TABLE rect (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',  -- new column <==========
    width int unsigned NOT NULL COMMENT 'Width',
    height int unsigned NOT NULL COMMENT 'Height',
    CONSTRAINT pk_rect PRIMARY KEY (id),
    CONSTRAINT fk_rect_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE  -- new FK constraint <==========
) Engine=InnoDB COMMENT 'Rectangles';
</code>
<para>Add the table corresponding to the new class, defined in the same way as the sample table above, to the database creation script. The new table must have an <c>id</c> column that links it to the base table.</para>
<code>
CREATE TABLE coloredrect (
    id int unsigned NOT NULL, -- PK is the same as base PK
    color int unsigned COMMENT 'Color',
    CONSTRAINT pk_rect PRIMARY KEY (id),
    CONSTRAINT fk_coloredrect_rect FOREIGN KEY (id) REFERENCES rect(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Rectangles';
</code>

<para><strong>(3)</strong> Create a new database upgrade script that contains modification that produce the same situation as defined in the database creation script.<br/>Add the same <c>add_type</c> calls from the database creation script to the upgrade script and alter the base table adding a type reference to the base table.</para>
<code>
CALL add_type(NULL, 'Terradue.Portal.Rect, Terradue.Portal', NULL, 'Rectangle', 'Rectangles', 'rectangles');
CALL add_type(NULL, 'Terradue.Portal.ColoredRect, Terradue.Portal', 'Terradue.Portal.Rect, Terradue.Portal', 'Colored rectangle', 'Colored rectangles', NULL);

-- Changing structure of table "rect" (adding type reference) ...\
ALTER TABLE rect
    ADD COLUMN id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension' AFTER id,
    ADD CONSTRAINT fk_rect_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
;
-- RESULT
</code>
<para>Note: the comments before and after the <c>ALTER TABLE</c> command are used by Terradue.Admin.Tool that will runs this script for a user-friendly output.</para>
<para>Finally, add the same <c>CREATE TABLE</c> command from the database creation script.</para>



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

