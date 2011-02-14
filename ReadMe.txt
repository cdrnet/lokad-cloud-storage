Lokad.Cloud is an O/C mapper (object to cloud) for Windows Azure.
Copyright (c) Lokad SAS, 2009-2011

Authors: Joannes Vermorel, Christoph Ruegg, Rinat Abdullin

Project page: http://code.google.com/p/lokad-cloud/
Company page: http://www.lokad.com/
Community Forums: http://ask.lokad.com/ 

BIG PICTURE

Lokad.Cloud comes with two .NET 4.0 assemblies:
- Lokad.Cloud.Storage.dll, the O/C mapper which can be used alone.
- Lokad.Cloud.Framework.dll the execution framework (depend on storage).

Those assemblies is typically referenced in two places:
- in the client app (eventually a web app) pushing and retrieving work (storage only).
- in the library containing your cloud services for back-end processing (both assemblies).

The O/C mapper is a stand-alone library.
The framework depends on Autofac v1.

The Autofac dependency is also open source, see 
http://code.google.com/p/autofac/


GETTING STARTED

Please refer to the instructions at
http://code.google.com/p/lokad-cloud/wiki/GettingStarted
