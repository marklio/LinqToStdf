#Good News
Good news, everyone!  LinqToStdf is now using Git, and we'll happily take pull requests that line up with the principles of the project.  We owe you guys a list of those things, but we'll happily take a look at requests and let you know until we have them documented. ;)
#Project Description
A library for parsing/processing Standard Test Datalog Format (STDF) files, typically used in semiconductor testing.
#Features
* Parsing of the general STDF file structure
* Support for Linq style queries over STDF files
* Specific support for the STDF V4 spec, including "structured" extensions.  For example, get all the Parametric Test Records for a given Part (from PIR or PRR).
* Pluggable record registration.  Plug in parsers for your custom records, or describe them 
* Parsing of the general STDF file structure
* Support for Linq style queries over STDF files
* Specific support for the STDF V4 spec, including "structured" extensions.  For example, get all the Parametric Test Records for a given Part (from PIR or PRR).
* Pluggable record registration.  Plug in parsers for your custom records, or describe them via attributes and let the library create the parsers for you on the fly.
* Tolerance for corrupt/malformed files
  * Pluggable policy for errors.  For example, you can throw on any errors, or take other actions appropriate for your scenario like repair.
  * Pluggable corruption detection and recovery
* Generation of "missing" data (such as part/bin/test summaries)
* High performance, tunable for a broad range of scenarios
* STDF file generation, especially as a result of processing existing files.
* Pluggable filters, allowing a wide range of behavior such as data transform
  * Built-in filters for things like synthesizing summary records and enforcing STDF V4 record ordering.
* "Pre-compiled" queries, allowing you to leverage the richness of the API and the performance of a single-use parser.

##[We need corrupt STDFs]!

#General Overview
For a general overview, go see the [Basic Idea]

#Motivation
Discover the [Motivation] behind the library.

#Example Usage
See [Example Usage]
