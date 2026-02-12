# API Docs Spec

## Overview
API reference site and topical guides for Agibuild.Avalonia.WebView.

## Requirements

### AD-1: XML doc generation
- GenerateDocumentationFile enabled globally (Directory.Build.props)
- Disabled for tests and benchmarks

### AD-2: docfx configuration
- docfx.json for Core, Runtime, WebView projects
- Metadata from project files; flattened namespace layout
- Modern template; output to _site

### AD-3: Getting Started guide
- Prerequisites, Quick Start (template), Manual Setup, Navigation

### AD-4: Topic guides
- Bridge guide: [JsExport]/[JsImport], bridge usage
- SPA Hosting guide: embedded resources, dev proxy
- Architecture guide: design overview
