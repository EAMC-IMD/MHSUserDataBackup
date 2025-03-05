# UserDataManagement

## Intent

UserDataManagement is intended to back up certain application specific data to OneDrive that is not normally captured. This includes bookmark/favorite data for Edge, Chrome, and Firefox; and profile data for StickyNotes, Outlook Signatures, AsUType, and Notepad++.

## Unattended Usage

### silentback

UserDataManagement.exe silentback

This will silently conduct backup operations.  It will not wait for OD sync to complete.

### silentrest

UserDataManagement.exe silentrest

This will silently conduct restore operations. It may overwrite existing data without confirmation.