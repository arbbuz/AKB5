using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseSpreadsheetMlWriter
    {
        private static readonly XNamespace SpreadsheetNamespace = "urn:schemas-microsoft-com:office:spreadsheet";

        public string BuildWorkbookXml(SavedData data)
        {
            var normalizedConfig = KnowledgeBaseDataService.NormalizeConfig(data.Config);
            var normalizedWorkshops = KnowledgeBaseDataService.NormalizeWorkshops(data.Workshops);
            string lastWorkshop = KnowledgeBaseDataService.ResolveWorkshop(normalizedWorkshops, data.LastWorkshop);

            XNamespace ns = SpreadsheetNamespace;
            XNamespace officeNs = "urn:schemas-microsoft-com:office:office";
            XNamespace excelNs = "urn:schemas-microsoft-com:office:excel";
            XNamespace htmlNs = "http://www.w3.org/TR/REC-html40";

            var workbook = new XElement(
                ns + "Workbook",
                new XAttribute(XNamespace.Xmlns + "o", officeNs),
                new XAttribute(XNamespace.Xmlns + "x", excelNs),
                new XAttribute(XNamespace.Xmlns + "ss", ns),
                new XAttribute(XNamespace.Xmlns + "html", htmlNs),
                BuildStyles(ns),
                CreateWorksheet(
                    ns,
                    "Meta",
                    new[] { "Property", "Value" },
                    BuildMetaRows(data.SchemaVersion, lastWorkshop)),
                CreateWorksheet(
                    ns,
                    "Levels",
                    new[] { "LevelIndex", "LevelName" },
                    BuildLevelRows(normalizedConfig)),
                CreateWorksheet(
                    ns,
                    "Workshops",
                    new[] { "WorkshopOrder", "WorkshopName", "IsLastSelected" },
                    BuildWorkshopRows(normalizedWorkshops, lastWorkshop)),
                CreateWorksheet(
                    ns,
                    "Nodes",
                    new[]
                    {
                        "NodeId",
                        "WorkshopName",
                        "ParentNodeId",
                        "SiblingOrder",
                        "LevelIndex",
                        "LevelName",
                        "NodeName",
                        "Path"
                    },
                    BuildNodeRows(normalizedConfig, normalizedWorkshops)));

            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XProcessingInstruction("mso-application", "progid=\"Excel.Sheet\""),
                workbook);

            using var writer = new Utf8StringWriter();
            document.Save(writer);
            return writer.ToString();
        }

        private static IEnumerable<IReadOnlyList<string>> BuildMetaRows(int schemaVersion, string lastWorkshop)
        {
            yield return new[] { "FormatId", KnowledgeBaseExcelExchangeService.WorkbookFormatId };
            yield return new[] { "FormatVersion", KnowledgeBaseExcelExchangeService.WorkbookFormatVersion.ToString(CultureInfo.InvariantCulture) };
            yield return new[] { "SchemaVersion", schemaVersion.ToString(CultureInfo.InvariantCulture) };
            yield return new[] { "LastWorkshop", lastWorkshop };
        }

        private static IEnumerable<IReadOnlyList<string>> BuildLevelRows(KbConfig config)
        {
            for (int index = 0; index < config.LevelNames.Count; index++)
            {
                yield return new[]
                {
                    index.ToString(CultureInfo.InvariantCulture),
                    config.LevelNames[index]
                };
            }
        }

        private static IEnumerable<IReadOnlyList<string>> BuildWorkshopRows(
            IReadOnlyDictionary<string, List<KbNode>> workshops,
            string lastWorkshop)
        {
            int order = 1;
            foreach (var workshop in workshops.Keys)
            {
                yield return new[]
                {
                    order.ToString(CultureInfo.InvariantCulture),
                    workshop,
                    string.Equals(workshop, lastWorkshop, StringComparison.Ordinal) ? "TRUE" : "FALSE"
                };
                order++;
            }
        }

        private static IEnumerable<IReadOnlyList<string>> BuildNodeRows(
            KbConfig config,
            IReadOnlyDictionary<string, List<KbNode>> workshops)
        {
            var rows = new List<IReadOnlyList<string>>();
            int nextNodeId = 1;

            foreach (var workshop in workshops)
            {
                for (int index = 0; index < workshop.Value.Count; index++)
                {
                    FlattenNode(
                        rows,
                        config,
                        workshop.Key,
                        workshop.Value[index],
                        parentNodeId: string.Empty,
                        siblingOrder: index + 1,
                        currentPath: workshop.Value[index].Name,
                        nextNodeId: ref nextNodeId);
                }
            }

            return rows;
        }

        private static void FlattenNode(
            ICollection<IReadOnlyList<string>> rows,
            KbConfig config,
            string workshopName,
            KbNode node,
            string parentNodeId,
            int siblingOrder,
            string currentPath,
            ref int nextNodeId)
        {
            string nodeId = nextNodeId.ToString(CultureInfo.InvariantCulture);
            nextNodeId++;

            rows.Add(new[]
            {
                nodeId,
                workshopName,
                parentNodeId,
                siblingOrder.ToString(CultureInfo.InvariantCulture),
                node.LevelIndex.ToString(CultureInfo.InvariantCulture),
                GetLevelName(config, node.LevelIndex),
                node.Name,
                $"{workshopName} / {currentPath}"
            });

            for (int childIndex = 0; childIndex < node.Children.Count; childIndex++)
            {
                var child = node.Children[childIndex];
                FlattenNode(
                    rows,
                    config,
                    workshopName,
                    child,
                    nodeId,
                    childIndex + 1,
                    $"{currentPath} / {child.Name}",
                    ref nextNodeId);
            }
        }

        private static string GetLevelName(KbConfig config, int levelIndex) =>
            config.LevelNames.Count > levelIndex
                ? config.LevelNames[levelIndex]
                : $"Ур. {levelIndex + 1}";

        private static XElement BuildStyles(XNamespace ns) =>
            new(
                ns + "Styles",
                new XElement(
                    ns + "Style",
                    new XAttribute(ns + "ID", "Default"),
                    new XAttribute(ns + "Name", "Normal")),
                new XElement(
                    ns + "Style",
                    new XAttribute(ns + "ID", "Header"),
                    new XElement(
                        ns + "Font",
                        new XAttribute(ns + "Bold", "1"))));

        private static XElement CreateWorksheet(
            XNamespace ns,
            string worksheetName,
            IReadOnlyList<string> headers,
            IEnumerable<IReadOnlyList<string>> rows)
        {
            return new XElement(
                ns + "Worksheet",
                new XAttribute(ns + "Name", worksheetName),
                new XElement(
                    ns + "Table",
                    CreateRow(ns, headers, isHeader: true),
                    rows.Select(row => CreateRow(ns, row, isHeader: false))));
        }

        private static XElement CreateRow(XNamespace ns, IEnumerable<string> values, bool isHeader)
        {
            return new XElement(
                ns + "Row",
                values.Select(value =>
                {
                    var cell = new XElement(
                        ns + "Cell",
                        new XElement(
                            ns + "Data",
                            new XAttribute(ns + "Type", "String"),
                            value));

                    if (isHeader)
                        cell.Add(new XAttribute(ns + "StyleID", "Header"));

                    return cell;
                }));
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
    }
}
