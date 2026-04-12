using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    internal sealed class KnowledgeBaseXlsxWriter
    {
        private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace OfficeDocumentRelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
        private static readonly XNamespace ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";

        public byte[] BuildWorkbookPackage(SavedData data)
        {
            var normalizedConfig = KnowledgeBaseDataService.NormalizeConfig(data.Config);
            var normalizedWorkshops = KnowledgeBaseDataService.NormalizeWorkshops(data.Workshops);
            string lastWorkshop = KnowledgeBaseDataService.ResolveWorkshop(normalizedWorkshops, data.LastWorkshop);
            var workshopExports = BuildWorkshopExports(normalizedWorkshops, lastWorkshop);
            string lastWorkshopId = workshopExports
                .Single(workshop => string.Equals(workshop.WorkshopName, lastWorkshop, StringComparison.Ordinal))
                .WorkshopId;

            var worksheets = new[]
            {
                new WorksheetDefinition(
                    "Meta",
                    new[] { "Property", "Value" },
                    BuildMetaRows(data.SchemaVersion, lastWorkshop, lastWorkshopId)),
                new WorksheetDefinition(
                    "Levels",
                    new[] { "LevelIndex", "LevelName" },
                    BuildLevelRows(normalizedConfig)),
                new WorksheetDefinition(
                    "Workshops",
                    new[] { "WorkshopOrder", "WorkshopName", "IsLastSelected", "WorkshopId" },
                    BuildWorkshopRows(workshopExports)),
                new WorksheetDefinition(
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
                        "Path",
                        "WorkshopId"
                    },
                    BuildNodeRows(normalizedConfig, workshopExports))
            };

            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteXmlEntry(archive, "[Content_Types].xml", BuildContentTypesXml(worksheets.Length));
                WriteXmlEntry(archive, "_rels/.rels", BuildRootRelationshipsXml());
                WriteXmlEntry(archive, "xl/workbook.xml", BuildWorkbookXml(worksheets));
                WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml(worksheets.Length));

                for (int index = 0; index < worksheets.Length; index++)
                {
                    WriteXmlEntry(
                        archive,
                        $"xl/worksheets/sheet{index + 1}.xml",
                        BuildWorksheetXml(worksheets[index]));
                }
            }

            return stream.ToArray();
        }

        private static IReadOnlyList<WorkshopExportRow> BuildWorkshopExports(
            IReadOnlyDictionary<string, List<KbNode>> workshops,
            string lastWorkshop)
        {
            var rows = new List<WorkshopExportRow>();
            int order = 1;

            foreach (var workshop in workshops)
            {
                rows.Add(new WorkshopExportRow(
                    WorkshopOrder: order,
                    WorkshopName: workshop.Key,
                    WorkshopId: $"W{order}",
                    IsLastSelected: string.Equals(workshop.Key, lastWorkshop, StringComparison.Ordinal),
                    RootNodes: workshop.Value));
                order++;
            }

            return rows;
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildMetaRows(
            int schemaVersion,
            string lastWorkshop,
            string lastWorkshopId)
        {
            yield return new[]
            {
                WorksheetCell.String("FormatId"),
                WorksheetCell.String(KnowledgeBaseExcelExchangeService.WorkbookFormatId)
            };
            yield return new[]
            {
                WorksheetCell.String("FormatVersion"),
                WorksheetCell.Number(KnowledgeBaseExcelExchangeService.WorkbookFormatVersion)
            };
            yield return new[]
            {
                WorksheetCell.String("SchemaVersion"),
                WorksheetCell.Number(schemaVersion)
            };
            yield return new[]
            {
                WorksheetCell.String("LastWorkshop"),
                WorksheetCell.String(lastWorkshop)
            };
            yield return new[]
            {
                WorksheetCell.String("LastWorkshopId"),
                WorksheetCell.String(lastWorkshopId)
            };
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildLevelRows(KbConfig config)
        {
            for (int index = 0; index < config.LevelNames.Count; index++)
            {
                yield return new[]
                {
                    WorksheetCell.Number(index),
                    WorksheetCell.String(config.LevelNames[index])
                };
            }
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildWorkshopRows(
            IEnumerable<WorkshopExportRow> workshops)
        {
            foreach (var workshop in workshops)
            {
                yield return new[]
                {
                    WorksheetCell.Number(workshop.WorkshopOrder),
                    WorksheetCell.String(workshop.WorkshopName),
                    WorksheetCell.Boolean(workshop.IsLastSelected),
                    WorksheetCell.String(workshop.WorkshopId)
                };
            }
        }

        private static IEnumerable<IReadOnlyList<WorksheetCell>> BuildNodeRows(
            KbConfig config,
            IEnumerable<WorkshopExportRow> workshops)
        {
            var rows = new List<IReadOnlyList<WorksheetCell>>();
            int nextNodeId = 1;

            foreach (var workshop in workshops)
            {
                for (int index = 0; index < workshop.RootNodes.Count; index++)
                {
                    FlattenNode(
                        rows,
                        config,
                        workshop.WorkshopName,
                        workshop.WorkshopId,
                        workshop.RootNodes[index],
                        parentNodeId: null,
                        siblingOrder: index + 1,
                        currentPath: workshop.RootNodes[index].Name,
                        nextNodeId: ref nextNodeId);
                }
            }

            return rows;
        }

        private static void FlattenNode(
            ICollection<IReadOnlyList<WorksheetCell>> rows,
            KbConfig config,
            string workshopName,
            string workshopId,
            KbNode node,
            int? parentNodeId,
            int siblingOrder,
            string currentPath,
            ref int nextNodeId)
        {
            int nodeId = nextNodeId;
            nextNodeId++;

            rows.Add(new[]
            {
                WorksheetCell.Number(nodeId),
                WorksheetCell.String(workshopName),
                parentNodeId.HasValue ? WorksheetCell.Number(parentNodeId.Value) : WorksheetCell.String(string.Empty),
                WorksheetCell.Number(siblingOrder),
                WorksheetCell.Number(node.LevelIndex),
                WorksheetCell.String(GetLevelName(config, node.LevelIndex)),
                WorksheetCell.String(node.Name),
                WorksheetCell.String($"{workshopName} / {currentPath}"),
                WorksheetCell.String(workshopId)
            });

            for (int childIndex = 0; childIndex < node.Children.Count; childIndex++)
            {
                var child = node.Children[childIndex];
                FlattenNode(
                    rows,
                    config,
                    workshopName,
                    workshopId,
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

        private static XDocument BuildContentTypesXml(int worksheetCount)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    ContentTypesNamespace + "Types",
                    new XElement(
                        ContentTypesNamespace + "Default",
                        new XAttribute("Extension", "rels"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                    new XElement(
                        ContentTypesNamespace + "Default",
                        new XAttribute("Extension", "xml"),
                        new XAttribute("ContentType", "application/xml")),
                    new XElement(
                        ContentTypesNamespace + "Override",
                        new XAttribute("PartName", "/xl/workbook.xml"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                    Enumerable.Range(1, worksheetCount).Select(index =>
                        new XElement(
                            ContentTypesNamespace + "Override",
                            new XAttribute("PartName", $"/xl/worksheets/sheet{index}.xml"),
                            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
        }

        private static XDocument BuildRootRelationshipsXml()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    PackageRelationshipsNamespace + "Relationships",
                    new XElement(
                        PackageRelationshipsNamespace + "Relationship",
                        new XAttribute("Id", "rId1"),
                        new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                        new XAttribute("Target", "xl/workbook.xml"))));
        }

        private static XDocument BuildWorkbookXml(IReadOnlyList<WorksheetDefinition> worksheets)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    SpreadsheetNamespace + "workbook",
                    new XAttribute(XNamespace.Xmlns + "r", OfficeDocumentRelationshipsNamespace),
                    new XElement(
                        SpreadsheetNamespace + "sheets",
                        worksheets.Select((worksheet, index) =>
                            new XElement(
                                SpreadsheetNamespace + "sheet",
                                new XAttribute("name", worksheet.Name),
                                new XAttribute("sheetId", index + 1),
                                new XAttribute(OfficeDocumentRelationshipsNamespace + "id", $"rId{index + 1}"))))));
        }

        private static XDocument BuildWorkbookRelationshipsXml(int worksheetCount)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    PackageRelationshipsNamespace + "Relationships",
                    Enumerable.Range(1, worksheetCount).Select(index =>
                        new XElement(
                            PackageRelationshipsNamespace + "Relationship",
                            new XAttribute("Id", $"rId{index}"),
                            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                            new XAttribute("Target", $"worksheets/sheet{index}.xml")))));
        }

        private static XDocument BuildWorksheetXml(WorksheetDefinition worksheet)
        {
            var allRows = new List<IReadOnlyList<WorksheetCell>>
            {
                worksheet.Headers.Select(WorksheetCell.String).ToArray()
            };
            allRows.AddRange(worksheet.Rows);

            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    SpreadsheetNamespace + "worksheet",
                    new XElement(
                        SpreadsheetNamespace + "sheetData",
                        allRows.Select((row, rowIndex) =>
                            new XElement(
                                SpreadsheetNamespace + "row",
                                new XAttribute("r", rowIndex + 1),
                                row.Select((cell, columnIndex) => CreateCell(cell, rowIndex + 1, columnIndex + 1)))))));
        }

        private static XElement CreateCell(WorksheetCell cell, int rowIndex, int columnIndex)
        {
            string cellReference = GetCellReference(rowIndex, columnIndex);

            return cell.Kind switch
            {
                WorksheetCellKind.Number => new XElement(
                    SpreadsheetNamespace + "c",
                    new XAttribute("r", cellReference),
                    new XElement(
                        SpreadsheetNamespace + "v",
                        cell.Value)),
                WorksheetCellKind.Boolean => new XElement(
                    SpreadsheetNamespace + "c",
                    new XAttribute("r", cellReference),
                    new XAttribute("t", "b"),
                    new XElement(
                        SpreadsheetNamespace + "v",
                        cell.Value)),
                _ => CreateInlineStringCell(cellReference, cell.Value)
            };
        }

        private static XElement CreateInlineStringCell(string cellReference, string value)
        {
            var text = new XElement(SpreadsheetNamespace + "t", value);
            if (value.Length != value.Trim().Length)
                text.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));

            return new XElement(
                SpreadsheetNamespace + "c",
                new XAttribute("r", cellReference),
                new XAttribute("t", "inlineStr"),
                new XElement(
                    SpreadsheetNamespace + "is",
                    text));
        }

        private static string GetCellReference(int rowIndex, int columnIndex) =>
            $"{GetColumnName(columnIndex)}{rowIndex.ToString(CultureInfo.InvariantCulture)}";

        private static string GetColumnName(int columnIndex)
        {
            if (columnIndex <= 0)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            var chars = new Stack<char>();
            int current = columnIndex;
            while (current > 0)
            {
                current--;
                chars.Push((char)('A' + (current % 26)));
                current /= 26;
            }

            return new string(chars.ToArray());
        }

        private static void WriteXmlEntry(ZipArchive archive, string path, XDocument document)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using var stream = entry.Open();
            document.Save(stream);
        }

        private sealed record WorksheetDefinition(
            string Name,
            IReadOnlyList<string> Headers,
            IEnumerable<IReadOnlyList<WorksheetCell>> Rows);

        private sealed record WorkshopExportRow(
            int WorkshopOrder,
            string WorkshopName,
            string WorkshopId,
            bool IsLastSelected,
            List<KbNode> RootNodes);

        private enum WorksheetCellKind
        {
            String,
            Number,
            Boolean
        }

        private readonly record struct WorksheetCell(WorksheetCellKind Kind, string Value)
        {
            public static WorksheetCell String(string value) => new(WorksheetCellKind.String, value);

            public static WorksheetCell Number(int value) =>
                new(WorksheetCellKind.Number, value.ToString(CultureInfo.InvariantCulture));

            public static WorksheetCell Boolean(bool value) => new(WorksheetCellKind.Boolean, value ? "1" : "0");
        }
    }
}
