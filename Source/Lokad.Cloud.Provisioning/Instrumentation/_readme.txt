Lokad.Cloud.Provisioning Instrumentation
========================================

Intention
---------

A pure and robust distributed cloud application is expecting
transient errors of all kind when accessing remote resources.
Our experience with Windows Azure shows that this expectation
is well justified.

Lokad.Cloud.Provisioning tries to automatically handle transient
errors and retry where it makes sense.

The automatic error handling shields the developer from
most of the complexity, yet sometimes it would still be interesting
to have some insights into internal behaviour. Instead of writing
some obsucure logs, Lokad.Cloud.Provisioning optionally publishes some
system events to arbitrary observers.

Usage
-----

Storage events implement the ICloudProvisioningEvent marker interface.

You can observe provisioning events in the following ways:

*  Implementing the ICloudProvisioningObserver interface and pass
   it as argument to the constructors of the provisioning classes

*  Implement IObserver<ICloudProvisioningEvent> one or more times,
   pass them as arguments to the constructor of the 
   CloudProvisionignInstrumentationSubject class and then pass that
   as argument to the constructors of the provisioning classes

*  Create a CloudProvisionignInstrumentationSubject instance and
   pass it as argument to the constructors of the provisioning classes.
   Then consume that instance with Rx (it implements
   IObservable<ICloudStorageEvent> and behaves like an Rx subject).

Code Samples
------------

Lokad.Cloud.Framework currently implements a class that subscribes
to the subject class using Rx to write warnings and debug messages
to the log: Lokad.Cloud.Diagnostics.CloudProvisioningLogger.