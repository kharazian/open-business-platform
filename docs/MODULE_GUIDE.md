# Module Guide

This guide explains how to create a new module for Open Business Platform.

The project uses a Modular Monolith architecture. Each module should own its own business logic, API endpoints, database tables, and UI pages where possible.

## Module Goals

A module should be:

- Easy to understand
- Easy to remove
- Easy to test
- Independent from other modules
- Connected to other modules only through contracts or events

## Backend Module Structure

Example module:

```txt
src/api/Modules/Forms/
  Domain/
  Application/
  Infrastructure/
  Api/