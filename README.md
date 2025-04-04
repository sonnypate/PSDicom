# PSDicom

## Summary

Various DICOM Powershell commands. All commands use the PSDicom.DICOM.Connection object to provide the connectivity details. The command list is basic now, but more commands are planned in the near future.

PSDicom uses the excellent [Fellow Oak DICOM](#https://github.com/fo-dicom/fo-dicom) libary.

## Contents

- [Get-DicomConnection](#get-dicomconnection)
- [Test-DicomConnection](#test-dicomconnection)
- [Get-ModalityWorklist](#get-modalityworklist)

## Get-DicomConnection
`Get-DicomConnection` is the command you will use to create a Connection object.

```Powershell
$Connection = Get-DicomConnection -CalledHost "www.dicomserver.co.uk" -CalledAET "DICOMSERVER"
```

This creates a `PSDicom.DICOM.Connection` object:

```Powershell
$Connection | Get-Member
```
```Dos
   TypeName: PSDicom.DICOM.Connection

Name        MemberType Definition
----        ---------- ----------
Equals      Method     bool Equals(System.Object obj)
GetHashCode Method     int GetHashCode()
GetType     Method     type GetType()
ToString    Method     string ToString()
CalledAET   Property   string CalledAET {get;set;}
CalledHost  Property   string CalledHost {get;set;}
CallingAET  Property   string CallingAET {get;set;}
Port        Property   int Port {get;set;}
UseTLS      Property   bool UseTLS {get;set;}
```

## Test-DicomConnection
`Test-DicomConnection` performs a c-echo to the called AET. You have the option to log additional information to a file which may be useful when troubleshooting DICOM connections.

```Powershell
Test-DicomConnection -Connection $Connection -LogPath C:\Users\User1\Desktop\Test-DicomConnection.log
```

This example sends a single DICOM ping request. It will return the result to the console, and log additional information to the log file specified:

```Dos
Attempt    : 1
CallingAET : CALLINGAET
CalledHost : www.dicomserver.co.uk
CalledAET  : DICOMSERVER
Port       : 104
Status     : Success
Time       : 1064
```

## Get-ModalityWorklist
`Get-ModalityWorklist` performs a worklist query. You have the option to log additional information to a file which may be useful when troubleshooting a modality worklist.

```Powershell
Get-ModalityWorklist -Connection $Connection -StartDate '1/1/2001'
```

This example performs a worklist query against the called AET defined in the Connection object, with a specific date:

```Dos
PatientName          : Patient^Patient^^Miss
PatientId            : PAT003
Accession            : 126
Modality             : RF
ExamDescription      : Left Leg DSA
ScheduledStationAET  : IHWALSALIRIS
ScheduledStationName : Angio 1
ScheduledStudyDate   : 20010101
ScheduledStudyTime   : 082000.000000
StudyInstanceUID     : 1.2.826.0.1.3680043.11.103

PatientName          : Patient^Test^^Mrs
PatientId            : PAT002
Accession            : 124
Modality             : MR
ExamDescription      : MRI Left Shoulder
ScheduledStationAET  : MR1
ScheduledStationName : MR1
ScheduledStudyDate   : 20010101
ScheduledStudyTime   : 123000.000000
StudyInstanceUID     : 1.2.826.0.1.3680043.11.102
```