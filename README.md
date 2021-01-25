# Specflow Plugins

## Json table generator

This is a plugin to generate an Example for scenarios or scenario outlines.

The Example is generated from a JSON file containing an array of objects. Each object is used to generate a row in the Example. At this moment the first object in the array is taken to generate the header.

To use the plugin simply create the JSON file named **testdata.json** in the project root folder and set the tag `jsontable` in the scenario or scenario outline. 


### Example
Feature
```gherkin
Feature: Do stuff
  In order to do some stuff
  As a client
  I want to make request to the endpoints

  @jsontable:endpoints
  Scenario Outline: Access endpoints
    Given an api
    When make a request with method <method> to the endpoint <endpoint>
    Then gets response code <responseCode>
```
**testdata.json** file
```json
{
  "endpoints": [
    {
      "method": "GET",
      "endpoint": "api/endpoint1?id=valid",
      "responseCode": "200"
    },
    {
      "method": "GET",
      "endpoint": "api/endpoint1?id=invalid",
      "responseCode": "400"
    }
  ]
}
```
