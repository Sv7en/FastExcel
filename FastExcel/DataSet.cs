﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FastExcel
{
    public class DataSet
    {
        public IEnumerable<Row> Rows { get; set; }

        public IEnumerable<string> Headings { get; set; }

        public DataSet() { }

        public void PopulateRows<T>(IEnumerable<T> rows, int existingHeadingRows = 0, bool usePropertiesAsHeadings = false)
        {
            if ((rows.FirstOrDefault() as IEnumerable<object>) == null)
            {
                PopulateRowsFromObjects(rows, existingHeadingRows, usePropertiesAsHeadings);
            }
            else
            {
                PopulateRowsFromIEnumerable(rows as IEnumerable<IEnumerable<object>>, existingHeadingRows);
            }
        }

        private void PopulateRowsFromObjects<T>(IEnumerable<T> rows, int existingHeadingRows = 0, bool usePropertiesAsHeadings = false)
        {
            int rowNumber = existingHeadingRows + 1;

            // Get all properties
            PropertyInfo[] properties = typeof(T).GetProperties();
            List<Row> newRows = new List<Row>();
            
            if (usePropertiesAsHeadings)
            {
                this.Headings = (from prop in properties
                                 select prop.Name);

                int headingColumnNumber = 1;
                IEnumerable<Cell> headingCells = (from h in this.Headings
                                                   select new Cell(headingColumnNumber++, h)).ToArray();

                Row headingRow = new Row(rowNumber++, headingCells);

                newRows.Add(headingRow);
            }

            foreach (T rowObject in rows)
            {
                List<Cell> cells = new List<Cell>();
                
                int columnNumber = 1;

                // Get value from each property
                foreach (PropertyInfo propertyInfo in properties)
                {
                    object value = propertyInfo.GetValue(rowObject, null);
                    if(value != null)
                    {
                        Cell cell = new Cell(columnNumber, value);
                        cells.Add(cell);
                    }
                    columnNumber++;
                }

                Row row = new Row(rowNumber++, cells);
                newRows.Add(row);
            }

            this.Rows = newRows;
        }

        private void PopulateRowsFromIEnumerable(IEnumerable<IEnumerable<object>> rows, int existingHeadingRows = 0)
        {
            int rowNumber = existingHeadingRows + 1;
            
            List<Row> newRows = new List<Row>();

            foreach (IEnumerable<object> rowOfObjects in rows)
            {
                List<Cell> cells = new List<Cell>();

                int columnNumber = 1;

                foreach (object value in rowOfObjects)
                {
                    if (value != null)
                    {
                        Cell cell = new Cell(columnNumber, value);
                        cells.Add(cell);
                    }
                    columnNumber++;
                }

                Row row = new Row(rowNumber++, cells);
                newRows.Add(row);
            }

            this.Rows = newRows;
        }

        public void AddRow(params object[] cellValues)
        {
            if (this.Rows == null)
            {
                this.Rows = new List<Row>();
            }

            List<Cell> cells = new List<Cell>();

            int columnNumber = 1;
            foreach (object value in cellValues)
	        {
                if (value != null)
                {
		            Cell cell = new Cell(columnNumber++, value);
                    cells.Add(cell);
                }
                else
                {
                    columnNumber++;
                }
	        }

            Row row = new Row(this.Rows.Count() + 1, cells);
            (this.Rows as List<Row>).Add(row);
        }

        /// <summary>
        /// Note: This method is slow
        /// </summary>
        public void AddValue(int rowNumber, int columnNumber, object value)
        {
            if (this.Rows == null)
            {
                this.Rows = new List<Row>();
            }

            Row row = (from r in this.Rows
                       where r.RowNumber == rowNumber
                       select r).FirstOrDefault();
            Cell cell = null;

            if (row == null)
            {
                cell = new Cell(columnNumber, value);
                row = new Row(rowNumber, new List<Cell>{ cell });
                (this.Rows as List<Row>).Add(row);
            }

            if (cell == null)
            {
                cell = (from c in row.Cells
                        where c.ColumnNumber == columnNumber
                        select c).FirstOrDefault();

                if (cell == null)
                {
                    cell = new Cell(columnNumber, value);
                    (row.Cells as List<Cell>).Add(cell);
                }
            }

        }

        /// <summary>
        /// Merges the parameter into the current DatSet object, the parameter takes precedence
        /// </summary>
        /// <param name="data">A DataSet to merge</param>
        public void Merge(DataSet data)
        {
            // Merge headings
            if (this.Headings == null || !this.Headings.Any())
            {
                this.Headings = data.Headings;
            }

            // Merge rows
            data.Rows = MergeRows(data.Rows);
        }

        private IEnumerable<Row> MergeRows(IEnumerable<Row> rows)
        {
            foreach (var row in this.Rows.Union(rows).GroupBy(r => r.RowNumber))
            {
                int count = row.Count();
                if (count == 1)
                {
                    yield return row.First();
                }
                else
                {
                    row.First().Merge(row.Skip(1).First());

                    yield return row.First();
                }
            }
        }
    }
}
