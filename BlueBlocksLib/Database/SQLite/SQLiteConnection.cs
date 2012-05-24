﻿// -----------------------------------------------------------------------
// <copyright file="SQLite.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace BlueBlocksLib.Database {
	using System;
	using System.Collections.Generic;
	using System.Text;
	using BlueBlocksLib.Database.SQLite;
	using BlueBlocksLib.TypeUtils;
	using System.Reflection;
	using BlueBlocksLib.SetUtils;


	public class Result<T> : IEnumerable<T> where T : new() {

		FieldInfo[] fis;
		IntPtr db;
		string SQL;
		string order;
		string limit;
		string offset;
		Dictionary<string, object> conditions = new Dictionary<string, object>();
		internal Result(FieldInfo[] fis, IntPtr db, string SQL) {
			this.fis = fis;
			this.db = db;
			this.SQL = SQL;
		}

		private Result(FieldInfo[] fis, IntPtr db, string SQL, Dictionary<string, object> conditions)
			: this(fis, db, SQL) {
			this.conditions = conditions;
		}

		public Result<T> WhereEquals(string column, object cond) {
			return AddCondition(column, cond, "=");
		}

		public Result<T> WhereLike(string column, object cond) {
			return AddCondition(column, cond, "LIKE");
		}

		public Result<T> WhereMoreThan(string column, object cond) {
			return AddCondition(column, cond, ">");
		}

		public Result<T> WhereLessThan(string column, object cond) {
			return AddCondition(column, cond, "<");
		}

		public Result<T> WhereMoreThanOrEqual(string column, object cond) {
			return AddCondition(column, cond, ">=");
		}

		public Result<T> WhereLessThanOrEqual(string column, object cond) {
			return AddCondition(column, cond, "<=");
		}

		Result<T> AddCondition(string column, object cond, string operatorSymbol) {
			Dictionary<string, object> newConditions = new Dictionary<string, object>(conditions);
			string condname = "@param" + newConditions.Count;
			newConditions.Add(condname, cond);
			return new Result<T>(fis, db, SQL + " AND " + column + " " + operatorSymbol + " " + condname, newConditions);
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator() {

			// We prepare the SQL here, where we actually need it
			IntPtr stmt = SQLite3.Prepare2(db, SQL);
			foreach (var cond in conditions) {
				int paramIndex = SQLite3.BindParameterIndex(stmt, cond.Key);
				var bindresult = SQLite3.GetBindFunc(SQLite3.GetSQLType(cond.Value.GetType()))(stmt, paramIndex, cond.Value);
				SQLite3.CheckResult(db, bindresult);
			}

			// Step through each row and return as you go
			while (SQLite3.Step(stmt) == SQLite3.Result.Row) {
				object instanceBox = Activator.CreateInstance(typeof(T));
				for (int i = 0; i < fis.Length; i++) {
					fis[i].SetValue(instanceBox, SQLite3.GetReadFunc(fis[i].FieldType)(stmt, i));
				}
				yield return (T)instanceBox;
			}

			// Finally get rid of the statement
			SQLite3.Finalize(stmt);
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion
	}


	/// <summary>
	/// A database connection to an SQLite file
	/// </summary>
	public class SQLiteConnection :IDisposable {

		IntPtr db;
		public SQLiteConnection(string conn) {
			var result = SQLite3.Open(conn, out db, SQLiteFlags.SQLITE_OPEN_CREATE | SQLiteFlags.SQLITE_OPEN_READWRITE, IntPtr.Zero);
			SQLite3.CheckResult(db, result);
		}

		public void CreateTable<T>(string name) {
			var fis = typeof(T).GetFields();
			var fieldList = ArrayUtils.ConvertAll(fis, x => x.Name + " " + SQLite3.GetSQLType(x.FieldType));
			var stringFieldList = string.Join(", ", fieldList);

			IntPtr stmt = SQLite3.Prepare2(db, "CREATE TABLE IF NOT EXISTS " + name + " (rowid INTEGER PRIMARY KEY AUTOINCREMENT, " + stringFieldList + ") ");
			var result = SQLite3.Step(stmt);
			SQLite3.CheckResult(db, result);
			SQLite3.Finalize(stmt);
		}

		public void Insert<T>(string table, T values) {

			var fis = typeof(T).GetFields();
			var fieldList = ArrayUtils.ConvertAll(fis, x => x.Name);
			var valueList = ArrayUtils.ConvertAll(fis, x => "@" + x.Name);

			string SQL = "INSERT INTO " + table + " (" + string.Join(", ", fieldList) + ") VALUES ( " + string.Join(", ", valueList) + " )";
			IntPtr stmt = SQLite3.Prepare2(db, SQL);
			foreach (var fi in fis) {
				int paramIndex = SQLite3.BindParameterIndex(stmt, "@" + fi.Name);
				var bindresult = SQLite3.GetBindFunc(SQLite3.GetSQLType(fi.FieldType))(stmt, paramIndex, fi.GetValue(values));
				SQLite3.CheckResult(db, bindresult);
			}
			var stepresult = SQLite3.Step(stmt);
			SQLite3.CheckResult(db, stepresult);
			SQLite3.Finalize(stmt);
		}

		public Result<T> Select<T>(string table) where T : new() {
			var fis = typeof(T).GetFields();
			var fieldList = ArrayUtils.ConvertAll(fis, x => x.Name);
			string SQL = "SELECT " + string.Join(", ", fieldList) + " FROM " + table + " WHERE 1=1 ";
			return new Result<T>(fis, db, SQL);
		}

		#region IDisposable Members

		bool disposed = false;
		public void Dispose() {
			if (!disposed) {
				SQLite3.Close(db);
			}
		}

		#endregion
	}

}