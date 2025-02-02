# Clean-Architecture-Designer-using-Roslyn-
Design and implement fully Clean Archticture Project 
# ProjectMaker API Documentation

## Overview

ProjectMaker is a web API designed to facilitate the creation and management of clean architecture projects. It offers endpoints for creating projects, managing services (like web APIs or background services), and manipulating entities, complex types, properties, DTOs, and relationships.

## Endpoints

### BaseCreator

- **POST** `/api/BaseCreator/generate_bases`
  - Generates the base structure for a new service.
  - **Request Body:** `ServiceDto`

### Data

- **POST** `/api/Data/add_db_context`
  - Adds a new DB context to the project.
  - **Request Body:** `ServiceDto`

- **POST** `/api/Data/create_model`
  - Creates a new model within the project.
  - **Request Body:** `ModelDto`

- **POST** `/api/Data/get_entities`
  - Retrieves all entities in the service.
  - **Request Body:** `ServiceDto`

- **POST** `/api/Data/get_complex_types`
  - Retrieves all complex types in the service.
  - **Request Body:** `ServiceDto`

- **POST** `/api/Data/remove_entity`
  - Removes an entity from the model.
  - **Request Body:** `ModelDto`

- **POST** `/api/Data/remove_complex_type`
  - Removes a complex type from the model.
  - **Request Body:** `ModelDto`

- **POST** `/api/Data/add_entity_properties`
  - Adds properties to an entity.
  - **Request Body:** `AddPropertyDto`

- **POST** `/api/Data/get_entity_properties`
  - Retrieves properties of an entity.
  - **Request Body:** `ModelDto`

- **POST** `/api/Data/remove_entity_properties`
  - Removes properties from an entity.
  - **Request Body:** `DeletePropertyDto`

- **POST** `/api/Data/add_complex_type_properties`
  - Adds properties to a complex type.
  - **Request Body:** `AddPropertyDto`

- **POST** `/api/Data/get_complex_type_properties`
  - Retrieves properties of a complex type.
  - **Request Body:** `ModelDto`

- **POST** `/api/Data/remove_complex_type_properties`
  - Removes properties from a complex type.
  - **Request Body:** `DeletePropertyDto`

### DbHandler

- **POST** `/api/DbHandler/add_valiadation_attribute`
  - Adds a validation attribute to a property.
  - **Request Body:** `AttributeDto`

- **POST** `/api/DbHandler/get_attribute_details`
  - Retrieves details of a validation attribute.
  - **Request Body:** `AttributeDto`

- **POST** `/api/DbHandler/remove_attribute`
  - Removes a validation attribute from a property.
  - **Request Body:** `AttributeDto`

### Dto

- **POST** `/api/Dto/create_dto`
  - Creates a new DTO for an entity.
  - **Request Body:** `AddDtoProperties`

- **POST** `/api/Dto/get_dtos`
  - Retrieves all DTOs in the service.
  - **Request Body:** `ServiceDto`

- **POST** `/api/Dto/get_dto_properties`
  - Retrieves properties of a DTO.
  - **Request Body:** `ModelDto`

- **POST** `/api/Dto/remove_dto_properties`
  - Removes properties from a DTO.
  - **Request Body:** `DeleteDtoProperties`

### Project

- **POST** `/api/Project/create_new_project`
  - Creates a new project.
  - **Request Body:** `ProjectDto`

- **POST** `/api/Project/remove_project`
  - Removes a project.
  - **Request Body:** `ProjectDto`

- **POST** `/api/Project/get_projects`
  - Retrieves all projects.

- **POST** `/api/Project/create_new_service`
  - Creates a new service within a project.
  - **Request Body:** `ServiceDto`

- **POST** `/api/Project/remove_service`
  - Removes a service from a project.
  - **Request Body:** `ServiceDto`

- **POST** `/api/Project/get_services`
  - Retrieves all services within a project.
  - **Request Body:** `ProjectDto`

### RelationShip

- **POST** `/api/RelationShip/add_one_to_one_relationship`
  - Adds a one-to-one relationship between entities.
  - **Request Body:** `OneToOneRelationshipDto`

- **POST** `/api/RelationShip/remove_one_to_one_relationship`
  - Removes a one-to-one relationship between entities.
  - **Request Body:** `OneToOneRelationshipDto`

- **POST** `/api/RelationShip/add_one_to_many_relationship`
  - Adds a one-to-many relationship between entities.
  - **Request Body:** `OneToManyRelationshipDto`

- **POST** `/api/RelationShip/remove_one_to_many_relationship`
  - Removes a one-to-many relationship between entities.
  - **Request Body:** `OneToManyRelationshipDto`

- **POST** `/api/RelationShip/add_many_to_many_relationship`
  - Adds a many-to-many relationship between entities.
  - **Request Body:** `ManyToManyRelationshipDto`

- **POST** `/api/RelationShip/remove_many_to_many_relationship`
  - Removes a many-to-many relationship between entities.
  - **Request Body:** `ManyToManyRelationshipDto`

### ServiceCreator

- **POST** `/api/ServiceCreator/create_service_contracts`
  - Creates service contracts for a service.
  - **Request Body:** `ServiceDto`

- **POST** `/api/ServiceCreator/create_service_implementations`
  - Creates service implementations for a service.
  - **Request Body:** `ServiceDto`

- **POST** `/api/ServiceCreator/add_mapping`
  - Adds mapping configurations for a service.
  - **Request Body:** `ServiceDto`

## Schemas

```json
{
  "projectName": "string",
  "serviceName": "string",
  "serviceType": "integer"
}
{
  "projectName": "string",
  "serviceName": "string",
  "modelName": "string",
  "modelType": "integer"
}
{
  "projectName": "string",
  "serviceName": "string",
  "modelName": "string",
  "dtoType": "integer",
  "properties": [
    {
      "name": "string",
      "type": "string"
    }
  ]
}
{
  "projectName": "string",
  "servicename": "string",
  "modelName": "string",
  "properties": [
    {
      "name": "string",
      "type": "string"
    }
  ]
}
{
  "projectName": "string",
  "serviceName": "string",
  "modelName": "string",
  "propertyName": "string",
  "annotationType": "string",
  "value": "any",
  "errorMessage": "string"
}
{
  "projectName": "string",
  "serviceName": "string",
  "modelName": "string",
  "propertyNames": [
    "string"
  ]
}
{
  "projectName": "string",
  "serviceName": "string",
  "modelName": "string",
  "propertyNames": [
    "string"
  ]
}
{
  "projectName": "string",
  "serviceName": "string",
  "firstEntity": "string",
  "secondEntity": "string",
  "deleteRule": "integer"
}
{
  "projectName": "string",
  "serviceName": "string",
  "oneEntity": "string",
  "manyEntity": "string",
  "isMandatory": "boolean",
  "deleteRule": "integer"
}
{
  "projectName": "string",
  "serviceName": "string",
  "sourceEntity": "string",
  "targetEntity": "string",
  "isMandatory": "boolean",
  "deleteRule": "integer"
}
{
  "projectName": "string"
}
{
  "name": "string",
  "type": "string"
}
