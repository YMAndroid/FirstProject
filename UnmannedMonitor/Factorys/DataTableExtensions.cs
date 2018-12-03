using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Factorys
{
    public static class DataTableExtensions
    {
        /// <summary>
        /// List<T>转DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this List<T> items)
        {

            //var ret = new DataTable();
            //foreach (PropertyDescriptor dp in TypeDescriptor.GetProperties(typeof(T)))
            //    ret.Columns.Add(dp.Name);
            //foreach (T item in items)
            //{
            //    var Row = ret.NewRow();
            //    foreach (PropertyDescriptor dp in TypeDescriptor.GetProperties(typeof(T)))
            //        Row[dp.Name] = dp.GetValue(item);
            //    ret.Rows.Add(Row);
            //}
            //return ret;
            //DataTable result = new DataTable();
            //List<PropertyInfo> pList = new List<PropertyInfo>();
            //Type type = typeof(T);
            //Array.ForEach<PropertyInfo>(type.GetProperties(), prop => { pList.Add(prop); result.Columns.Add(prop.Name, prop.PropertyType); });
            //foreach (var item in list)
            //{
            //    DataRow row = result.NewRow();
            //    pList.ForEach(p => row[p.Name] = p.GetValue(item, null));
            //    result.Rows.Add(row);
            //}
            //return result;

            DataTable dataTable = new DataTable();

            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                dataTable.Columns.Add(prop.Name, prop.PropertyType);
            }

            foreach (T obj in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    values[i] = Props[i].GetValue(obj, null);
                }
                dataTable.Rows.Add(values);
            }

            return dataTable;


            //检查实体集合不能为空
            //if (entitys == null || entitys.Count < 1)
            //{
            //    throw new Exception("需转换的集合为空");
            //}
            ////取出第一个实体的所有Propertie
            //Type entityType = entitys[0].GetType();
            //PropertyInfo[] entityProperties = entityType.GetProperties();

            ////生成DataTable的structure
            ////生产代码中，应将生成的DataTable结构Cache起来，此处略
            //DataTable dt = new DataTable();
            //for (int i = 0; i < entityProperties.Length; i++)
            //{
            //    //dt.Columns.Add(entityProperties[i].Name, entityProperties[i].PropertyType);
            //    dt.Columns.Add(entityProperties[i].Name);
            //}
            ////将所有entity添加到DataTable中
            //foreach (object entity in entitys)
            //{
            //    //检查所有的的实体都为同一类型
            //    if (entity.GetType() != entityType)
            //    {
            //        throw new Exception("要转换的集合元素类型不一致");
            //    }
            //    object[] entityValues = new object[entityProperties.Length];
            //    for (int i = 0; i < entityProperties.Length; i++)
            //    {
            //        entityValues[i] = entityProperties[i].GetValue(entity, null);
            //    }
            //    dt.Rows.Add(entityValues);
            //}
            //return dt;
        }

        /// <summary>
        /// DataTable转List<T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable table) where T : class, new()
        {
            List<T> result = new List<T>();
            List<PropertyInfo> pList = new List<PropertyInfo>();
            Type type = typeof(T);
            Array.ForEach<PropertyInfo>(type.GetProperties(), prop => { if (table.Columns.IndexOf(prop.Name) != -1) pList.Add(prop); });
            foreach (DataRow row in table.Rows)
            {
                T obj = new T();
                pList.ForEach(prop => { if (row[prop.Name] != DBNull.Value) prop.SetValue(obj, row[prop.Name], null); });
                result.Add(obj);
            }
            return result;
        }
    }
}
