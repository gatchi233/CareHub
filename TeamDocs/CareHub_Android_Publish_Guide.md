# CareHub Android Publish Guide

This guide builds the React Native Android app from Windows.

## 1. Prerequisites

- Install Android Studio or Android command-line tools.
- Install Java from Android Studio JBR or another JDK.
- Run `npm.cmd install` once in `CareHub.Mobile.ReactNative`.
- Confirm the API is online:

```powershell
Invoke-WebRequest https://carehub-production-fae0.up.railway.app/health -UseBasicParsing
```

The mobile API base URL should be:

```text
https://carehub-production-fae0.up.railway.app/api
```

## 2. Create A Local Release Keystore

Run from the repo root:

```powershell
cd C:\src\CareHub\CareHub.Mobile.ReactNative
New-Item -ItemType Directory -Force .\android\keystores
& "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe" -genkeypair -v -storetype PKCS12 -keystore ".\android\keystores\carehub-release.keystore" -alias carehub-release -keyalg RSA -keysize 2048 -validity 10000
```

Use a password you will not lose. Android updates must be signed with the same key.

## 3. Add Local Signing Settings

Create `CareHub.Mobile.ReactNative/android/keystore.properties`.

```properties
storeFile=keystores/carehub-release.keystore
storePassword=YOUR_KEYSTORE_PASSWORD
keyAlias=carehub-release
keyPassword=YOUR_KEY_PASSWORD
```

Do not commit `keystore.properties` or the keystore file. They are ignored by Git.

## 4. Build A Signed APK

```powershell
cd C:\src\CareHub\CareHub.Mobile.ReactNative\android
.\gradlew.bat assembleRelease
```

Output:

```text
C:\src\CareHub\CareHub.Mobile.ReactNative\android\app\build\outputs\apk\release\app-release.apk
```

Use this APK for direct install, GitHub Releases, or a presentation demo.

## 5. Build A Play Store Bundle

```powershell
cd C:\src\CareHub\CareHub.Mobile.ReactNative\android
.\gradlew.bat bundleRelease
```

Output:

```text
C:\src\CareHub\CareHub.Mobile.ReactNative\android\app\build\outputs\bundle\release\app-release.aab
```

Use the AAB for Google Play Console.

## 6. Install The Release APK On A Device

```powershell
& "C:\Users\sambe\AppData\Local\Android\Sdk\platform-tools\adb.exe" install -r C:\src\CareHub\CareHub.Mobile.ReactNative\android\app\build\outputs\apk\release\app-release.apk
```

Release APKs include the JavaScript bundle, so Metro does not need to be running.

