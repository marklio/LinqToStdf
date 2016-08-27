! Good News
Good news, everyone!  LinqToStdf is now using Git, and we'll happily take pull requests that line up with the principles of the project.  We owe you guys a list of those things, but we'll happily take a look at requests and let you know until we have them documented. ;)
! Project Description
A library for parsing/processing Standard Test Datalog Format (STDF) files, typically used in semiconductor testing.
! Features
* Parsing of the general STDF file structure
* Support for Linq style queries over STDF files
* Specific support for the STDF V4 spec, including "structured" extensions.  For example, get all the Parametric Test Records for a given Part (from PIR or PRR).
* Pluggable record registration.  Plug in parsers for your custom records, or describe them 
