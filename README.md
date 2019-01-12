# DataToolsUtils repository
This GitHub repository contains code of DataToolsUtils project - set of plugins helping you with development of database objects in MIcrosoft SQL Server Data Tools

## Plugins
The project currently contains only one plugin.

### DeploySingleSqlObject
This plugin helps you to deploy changes of only one SQL object currently opened in Visual Studio. This is especially helpful when you are developing only one object (eg. stored procedure) and your database project is too large so complete deployment of any change take several minutes.
This plugin simply drops and recreates the object on defined SQL server database.
There are several limitations - it can deploy only functions, procedures or views.

!!!BEWARE that DeploySingleSqlObject does not preserve permissions or extended properties!!! It drops the target object (which causes also drop of permissions, extended properties) and than creates the object again.

You can use the plugin easily by opening code of procedure (function, view) in Visual Studio. Than you have to click on DeploySingleSqlObject icon on DataToolsUtils toolbar in VisualStudio. You need to pick correct database connection in next dialog window.
Clicking OK will deploy the object. Results of deployment or any error messages will be displayed in DeploySingleSqlObject Output pane.

See wiki to get guide with screenshots https://github.com/HonzaSob/DataToolsUtils/wiki/DeploySingleSqlObject

## License
This project is licensed under the GNU GPL license. See the license.txt file in the root.

## Questions
Email questions to: jansobotka@seznam.cz.
