# LocationSampling

## Description
A sample dotnet MAUI application to demonstrate issues raised in issue ticket https://github.com/dotnet/maui/issues/28962

When using a timer to poll for locations using the Geolocation.GetLocationAsync method results in locations with poor accuracy on both iOS
and some Android devices. Using the Geolocation.StartListeningAsync method on iOS results in acceptable location accuracy. 

This app also demonstrates the behaiour reported in the issue https://github.com/dotnet/maui/issues/22683

Tested devices:

iPhone 11 (iOS 18)
iPhone 14 (iOS 18)
Samsung Galaxy A55 (Android 14)
Xiaomi Redmi Note 13 Pro (Android 14)
Google Pixle 6a (Android 15 - GrapheneOS)

## Features
Supports using both listening for location changes and polling for location changes.

On the Android platform the default method to sample locations is using polling. On iOS the default method is to
listen for location changes.

On sampling a location a JSON file is created and the file sharing system dialog will be displayed.



