# Shiny Extensions

## Dependency Injection Extensions

* Source generate all attributed classes to a single add file - saves you the boilerplate
* Extension methods for registering a dependency against multiple interfaces
* Extension methods for startup tasks (different to hosted services that don't work on mobile)

## Stores
* Key/value store with support for
  * Android/iOS/Windows - Preferences & Secure Storage
  * Web - Local Storage & Session Storage
  * In Memory
* Object binder binds INotifyPropertyChanged against a key/value store to persist object changes across sessions
* Simply implement IKeyValueStore to create your own store

## Web Hosting Extensions
* Merges service container build and post build scenarios into a single class
* All IInfrastructureModule implementations are automatically detected and run