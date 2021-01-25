using Gherkin.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Generator.Configuration;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Parser;

namespace Tapmiapp.JsonData.SpecFlowPlugin
{
    public interface IExternalDataFeaturePatcher
    {
        SpecFlowDocument PatchDocument(SpecFlowDocument feature);
    }

    public class ExternalDataFeaturePatcher : IExternalDataFeaturePatcher
    {
        public const string PROPERTY_TAG = "jsontable";
        private readonly ITagFilterMatcher _tagFilterMatcher;
        private readonly ITestDataProvider _testDataProvider;

        public ExternalDataFeaturePatcher(SpecFlowProjectConfiguration configuration, ITagFilterMatcher tagFilterMatcher, ITestDataProvider testDataProvider)
        {
            _tagFilterMatcher = tagFilterMatcher;
            _testDataProvider = testDataProvider;
        }

        public SpecFlowDocument PatchDocument(SpecFlowDocument originalSpecFlowDocument)
        {
            var feature = originalSpecFlowDocument.SpecFlowFeature;
            var scenarioDefinitions = feature.Children.Where(c => c is Background).ToList();

            foreach (var scenario in feature.ScenarioDefinitions.OfType<Scenario>())
            {

                if (!_tagFilterMatcher.GetTagValue(PROPERTY_TAG, scenario.Tags.Select(t => t.Name.Substring(1)),
                    out var tagString))
                {
                    scenarioDefinitions.Add(scenario);
                    continue;
                }

                var newScenarioOutline = PatchScenario(tagString, scenario);

                scenarioDefinitions.Add(newScenarioOutline);
            }

            var newDocument = CreateSpecFlowDocument(originalSpecFlowDocument, feature, scenarioDefinitions);
            return newDocument;
        }

        private ScenarioOutline PatchScenario(string tagString, Scenario scenario)
        {
            var testValues = _testDataProvider.TestData;

            var dict = testValues as Dictionary<string, object>;
            if (dict == null)
                throw new InvalidOperationException($"Cannot resolve properties from {testValues}");

            object entries;
            if (!dict.TryGetValue(tagString, out entries) &&
                    !dict.TryGetValue(tagString.Replace("_", " "), out entries))
                throw new InvalidOperationException($"Cannot resolve property {tagString}");

            var objList = (List<object>)entries;
            var list = objList.Select(e => (Dictionary<string, object>)e);

            var examples = new List<Examples>(scenario.Examples);

            if (list.Any())
            {
                var first = list.First();
                var headerCells = new List<TableCell>
                {
                    new TableCell(scenario.Location, "Variant")
                };

                var orderedKeys = new List<string>();
                foreach (var key in first.Keys)
                {
                    headerCells.Add(new TableCell(scenario.Location, key));
                    orderedKeys.Add(key); //to keep column order
                }

                var header = new TableRow(scenario.Location, headerCells.ToArray());

                var rows = new List<TableRow>();

                var variantId = 1;
                foreach (var entryDict in list)
                {
                    var rowCells = new List<TableCell>
                    {
                        new TableCell(scenario.Location, variantId.ToString())
                    };

                    variantId++;

                    foreach (var key in orderedKeys)
                    {
                        rowCells.Add(new TableCell(scenario.Location, entryDict[key].ToString()));
                    };

                    rows.Add(new TableRow(scenario.Location, rowCells.ToArray()));
                }

                examples.Add(new Examples(new Tag[0], scenario.Location, "Examples", string.Empty,
                    string.Empty, header, rows.ToArray()));
            }

            var newScenarioOutline = new ScenarioOutline(
                scenario.Tags.ToArray(),
                scenario.Location,
                scenario.Keyword,
                scenario.Name,
                scenario.Description,
                scenario.Steps.ToArray(),
                examples.ToArray());

            return newScenarioOutline;
        }

        private SpecFlowDocument CreateSpecFlowDocument(SpecFlowDocument originalSpecFlowDocument, SpecFlowFeature originalFeature, List<IHasLocation> scenarioDefinitions)
        {
            var newFeature = new SpecFlowFeature(originalFeature.Tags.ToArray(),
                                                 originalFeature.Location,
                                                 originalFeature.Language,
                                                 originalFeature.Keyword,
                                                 originalFeature.Name,
                                                 originalFeature.Description,
                                                 scenarioDefinitions.ToArray());

            var newDocument = new SpecFlowDocument(newFeature, originalSpecFlowDocument.Comments.ToArray(), originalSpecFlowDocument.DocumentLocation);
            return newDocument;
        }
    }
}
