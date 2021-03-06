namespace BuildDoc
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.IO;
    using Newtonsoft.Json.Linq;
    using System.Text.RegularExpressions;
    using BuildDoc.Entities;
    using BuildDoc.Logic;

    /// <summary>
    /// 文档操作主类
    /// </summary>
    public class DocMaster
    {
        private IBuildDocLogic BuildWordInstance
        {
            get { return BuildDocLogic.CreateInstance(); }
        }
             
        /// <summary>
        /// 文档生成接口
        /// </summary>
        private IBuildWord buildWord;

        /// <summary>
        /// 主ID
        /// </summary>
        private decimal masterID;

        /// <summary>
        /// 输入数据
        /// </summary>
        private string resultJson;

        /// <summary>
        /// inputJson传入的值覆盖配置生成的值
        /// </summary>
        public Dictionary<string, string> InputValue = new Dictionary<string, string>();

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName
        {
            get;
            set;
        }

        /// <summary>
        /// 构件信息列表
        /// </summary>
        public List<IDocStructure> StructureInfoList
        {
            get;
            set;
        }

        /// <summary>
        /// 输入数据
        /// </summary>
        public string ResultJson
        {
            get
            {
                return this.resultJson;
            }
        }

        /// <summary>
        /// 文件ID
        /// </summary>
        public decimal FileID
        {
            get;
            set;
        }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<BaseLabel> LabelList
        {
            get;
            set;
        }
        /// <summary>
        /// 是否合成报告文档
        /// </summary>
        public bool IsBuildDoc
        { get; set; }

        /// <summary>
        /// 模板类型
        /// </summary>
        internal DocTemplateType DocTemplateType
        {
            get;
            set;
        }

        /// <summary>
        /// 动态表单数据
        /// </summary>
        internal Dictionary<int, JArray> DynamicFormData
        {
            get;
            set;
        }
        /// <summary>
        /// 多数据源的值，避免重复查询
        /// </summary>
        internal Dictionary<string, string> MultiSourceValue
        { get; set; }

        /// <summary>
        /// 委托获取数据
        /// </summary>
        internal Func<decimal, string, string, decimal, string, Dictionary<string, string>, string> GetValueDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// 实例ID
        /// </summary>
        internal decimal InstanceID
        {
            get;
            set;
        }

        /// <summary>
        /// 初始化相关变量
        /// </summary>
        /// <param name="instanceDocumentID">实例文档ID</param>
        /// <param name="objID">对象ID</param>
        public DocMaster(decimal instanceDocumentID, decimal objID, bool isBuildDoc)
        {
            this.LabelList = new List<BaseLabel>();

            var docInstance = BuildWordInstance.GetInstanceDocument(instanceDocumentID);
            
            // 根据instanceDocumentID从数据库取出相关数据 masterID instanceID resultJson jsonStructure
            if (docInstance != null)
            {
                this.IsBuildDoc = isBuildDoc;
                this.masterID = docInstance.MOTHER_SET_ID.Value;
                this.InstanceID = objID;
                this.resultJson = docInstance.MANUAL_EDITING_RETURN;
                this.InitData(docInstance.DOCUMENT_STRUCTURE, null);
            }

        }

        /// <summary>
        /// 初始化相关变量
        /// </summary>
        /// <param name="masterID">主ID</param>
        /// <param name="instanceID">实例ID</param>
        /// <param name="resultJson">输入数据</param>
        public DocMaster(decimal masterID, decimal instanceID, string resultJson, bool isBuildDoc)
            : this(masterID, instanceID, resultJson, null, isBuildDoc)
        {
        }

        /// <summary>
        /// 初始化相关变量
        /// </summary>
        /// <param name="masterID">主ID</param>
        /// <param name="instanceID">实例ID</param>
        /// <param name="resultJson">输入数据</param>
        /// <param name="jsonStructure">构件集合</param>
        public DocMaster(decimal masterID, decimal instanceID, string resultJson, string jsonStructure, bool isBuildDoc) :
            this(masterID, instanceID, resultJson, jsonStructure, null, null, isBuildDoc)
        {
        }

        /// <summary>
        /// 初始化相关变量
        /// </summary>
        /// <param name="masterID">主ID</param>
        /// <param name="instanceID">实例ID</param>
        /// <param name="resultJson">输入数据</param>
        /// <param name="jsonStructure">构件集合</param>
        /// <param name="getValueDelegate">获取值函数</param>
        /// <param name="inputParams">输入参数</param>
        /// <param name="IsBuildDoc">是否是合成报告</param>
        public DocMaster(
            decimal masterID, 
            decimal instanceID, 
            string resultJson, 
            string jsonStructure,
            Func<decimal, string, string, decimal, string, Dictionary<string, string>, string> getValueDelegate, 
            Dictionary<string, string> inputParams,
            bool isBuildDoc
           )
        {
            this.IsBuildDoc = isBuildDoc;
            this.LabelList = new List<BaseLabel>();
            this.masterID = masterID;
            this.InstanceID = instanceID;
            this.resultJson = resultJson;
            this.GetValueDelegate = getValueDelegate;
            this.InitData(jsonStructure, inputParams);
         
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        /// <param name="jsonStructure">构件结构</param>
        /// <param name="inputParams">输入参数</param>
        private void InitData(string jsonStructure, Dictionary<string, string> inputParams)
        {
            try
            {
                MotherSetDTO motherSet = BuildWordInstance.GetMotherSet((int)this.masterID);
                if (motherSet != null)
                {
                    Dictionary<BlockType, List<Structure>> structureCofing;
                    this.FileID = motherSet.FILE_ID.Value;
                    this.DocTemplateType = new DocTemplateType(motherSet.TEMPLATE_TYPE.Value, this.InstanceID, inputParams);
                    var fileStream = FileServerHelper.GetFileStream(motherSet.FILE_ID.Value);
                    if (fileStream != null)
                        this.buildWord = new BuildWord(fileStream);
                    if (jsonStructure == null)
                    {
                        structureCofing = this.GetStructureDictionary(motherSet.SET_CONTENT);
                    }
                    else
                    {
                        structureCofing = this.GetStructureDictionary(jsonStructure);
                    }
                    //获取构建信息
                    this.StructureInfoList = this.GetStructureInfoList(structureCofing);
                }

                //处理返回结果
                if (!string.IsNullOrEmpty(this.resultJson))
                {
                    JArray ary = JArray.Parse(this.resultJson);
                    decimal id;
                    StructureType type;
                    foreach (var v in ary)
                    {
                        id = v["ID"].Value<decimal>();
                        type = (StructureType)Enum.Parse(typeof(StructureType), v["StructureType"].Value<string>());

                        if (!this.InputValue.ContainsKey(v["LabelName"].Value<string>()))
                        {
                            this.InputValue.Add(v["LabelName"].Value<string>(), v["Value"].Value<string>());
                        }
                    }
                }

                // 应用替换值
                if (this.InputValue != null && this.InputValue.Count > 0)
                {
                    this.LabelList.ForEach(label =>
                    {
                        if (label is TextLabel)
                        {
                            var textLabel = label as TextLabel;
                            var input = this.InputValue.FirstOrDefault(t => t.Key == label.LabelName);
                            if (!string.IsNullOrEmpty(input.Key))
                            {
                                textLabel.IsInput = true;
                                textLabel.Value = input.Value;
                            }
                        }
                    });
                }

                //处理条件标签
                // 1.这种判断有误，当条件标签的条件没有@标签的时候，内容不会被替换
                // 2.条件标签应该都算outside  modify by huzy 2016.4.5
                var inside = this.LabelList.Where(t => !t.RelateValue.Contains('@')).ToList();
                var outside = this.LabelList.Where(t => t.RelateValue.Contains('@')).ToList();
                var conditionS = this.LabelList.Where(t => t is ConditionLabel).ToList();
                foreach (var c in conditionS)
                {
                    inside.Remove(c);
                    if (!outside.Contains(c))
                        outside.Add(c);
                }

                var tmpList = new List<BaseLabel>();
                while (true)
                {
                    bool isBreak = true;
                    foreach (var oItem in outside)
                    {                        
                        foreach (var iItem in inside)
                        {
                            if (oItem.RelateValue.IndexOf(iItem.LabelName) > 0)
                            {
                                if (iItem is TextLabel)
                                {
                                    var textLabel = iItem as TextLabel;
                                    //var value = string.IsNullOrEmpty(textLabel.RelateValue) ? textLabel.GetValue() : textLabel.RelateValue;

                                    var value = textLabel.GetValue();
                                    if (!textLabel.IsAfterCompute)
                                        value = textLabel.InnerValue;

                                    bool pass = oItem.Replace(iItem.LabelName, value);
                                    if (!tmpList.Contains(oItem) && pass)
                                        tmpList.Add(oItem);
                                    if (isBreak && pass)
                                        isBreak = false;
                                }
                                else if (iItem is ConditionLabel) //条件引用条件标签
                                {
                                    var conditionLabel = iItem as ConditionLabel;
                                    BaseLabel baseLabel = conditionLabel.ConditionJudgment();
                                    if (baseLabel is TextLabel)
                                    {
                                        var textLabel = baseLabel as TextLabel;
                                        var value = textLabel.GetValue();
                                        bool pass = oItem.Replace(iItem.LabelName, value);
                                        if (!tmpList.Contains(oItem) && pass)
                                            tmpList.Add(oItem);
                                        if (isBreak && pass)
                                            isBreak = false;
                                    }
                                }
                            }
                        }
                    }
                    foreach (var item in tmpList)
                    {
                        inside.Add(item);
                        outside.Remove(item);
                    }
                    tmpList.Clear();
                    if (isBreak)
                        break;
                }

                //处理构建里无匹配的标签  匹配常量中的书名号《》
                this.LabelList.ForEach(label =>
                {
                    if (label is ConditionLabel)
                    {
                        var cl = label as ConditionLabel;
                        cl.LabelList.ForEach(l =>
                        {
                            string key = DocHelper.PatternString(l.Condition);
                            var findLable = inside.FirstOrDefault(i => i.LabelName == key);
                            if (findLable != null && findLable is TextLabel)
                            {
                                try
                                {
                                    var textLabel = findLable as TextLabel;
                                    var value = string.IsNullOrEmpty(textLabel.RelateValue) ? textLabel.GetValue() : textLabel.RelateValue;
                                    l.Condition = l.Condition.Replace("@" + key, value);
                                }
                                catch { }
                            }
                            if (DocHelper.CalcByJs(l.Condition) && l.BaseLabel is TextLabel)
                            {
                                var tl = l.BaseLabel as TextLabel;
                                tl.ReplaceWithConst(inside);
                            }
                        });
                    }

                    if (label is TextLabel)
                    {
                        var tl = label as TextLabel;
                        tl.ReplaceWithConst(inside);
                    }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据MasterID取出构建结构
        /// </summary>
        /// <param name="structureConfig">构件配置信息</param>
        /// <returns>构件信息列表</returns>
        private List<IDocStructure> GetStructureInfoList(Dictionary<BlockType, List<Structure>> structureConfig)
        {
            //decimal customerID = 0;
            List<IDocStructure> list = new List<IDocStructure>();

            foreach (var blockItem in structureConfig)
            {
                foreach (var structureItem in blockItem.Value)
                {
                    switch (structureItem.StructureType)
                    {
                        case StructureType.Config:
                            //生成word时不需要再判断构件的是否有效                               
                            if (this.IsBuildDoc || this.ConditionValid(structureItem.Condition, structureItem.Key))
                            {
                                var docStructure = new DocStructure(blockItem.Key, structureItem.Key, this);
                                docStructure.InitLabelList();
                                list.Add(docStructure);
                            }
                            break;
                        case StructureType.Custom:
                            var custStructure = new CustomDocStructure(blockItem.Key, structureItem.Key, this, structureItem.Key, structureItem.StructureName);
                            custStructure.InitLabelList();
                            list.Add(custStructure);
                            break;
                    }
                }
            }

            //foreach (IDocStructure item in list)
            //{
            //    if (item is CustomDocStructure)
            //    {
            //        item.InitLabelList();
            //    }
            //}

            return list;

        }

        /// <summary>
        /// 条件判断
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="structureID">构件ID</param>
        /// <returns>是否通过</returns>
        private bool ConditionValid(string condition, decimal structureID)
        {
            try
            {
                var result = false;
                if (!string.IsNullOrEmpty(condition))
                {

                    if (condition.Contains("@"))
                    {
                        string key = DocHelper.PatternString(condition);
                        if (this.DocTemplateType.ParamsCache.ContainsKey(key))
                        {
                            condition = condition.Replace("@" + key, this.DocTemplateType.ParamsCache[key]);
                        }
                    }

                    if (DocHelper.CalcByJs(condition))
                    {
                        result = true;
                    }

                }
                else
                {
                    result = true;
                }

                return result;
            }
            catch
            {

                throw new Exception("验证模板构件'" + structureID + "'的条件时出错");
            }
        }

         //<summary>
         //获取构件结构信息
         //</summary>
         //<param name="json">JSON构件结构信息</param>
         //<returns>构件结构信息</returns>
        private Dictionary<BlockType, List<Structure>> GetStructureDictionary(string json)
        {
          
            Dictionary<BlockType, List<Structure>> structureDictionary = new Dictionary<BlockType, List<Structure>>();
            JObject j_structureConfig = JObject.Parse(json);
            string ids = string.Empty;
            foreach (var blockItem in j_structureConfig)
            {
                List<Structure> structureList = new List<Structure>();
                structureDictionary.Add((BlockType)Enum.Parse(typeof(BlockType), blockItem.Key), structureList);
                JArray j_structureList = blockItem.Value as JArray;

                foreach (var j_structure in j_structureList)
                {
                    Structure structure = new Structure();
                    structure.StructureType = (StructureType)Enum.Parse(typeof(StructureType), j_structure["Type"].Value<string>());
                    structure.Key = j_structure["Key"].Value<decimal>();
                    structure.Condition = j_structure["Condition"].Value<string>();
                    structureList.Add(structure);
                }
            }

            return structureDictionary;
        }

        /// <summary>
        /// 没有调用到，暂时屏蔽 所有构建的书签
        /// </summary>
        /// <returns></returns>
        /*
        public List<string> GetBookmarks()
        {
            List<string> bookmarks = new List<string>();
            foreach (var structureInfoItem in this.StructureInfoList)
            {
                var bookmarksWithStructure = structureInfoItem.GetBookmarks();
                foreach (var bookmark in bookmarksWithStructure)
                {
                    if (!bookmarks.Contains(bookmark))
                    {
                        bookmarks.Add(bookmark);
                    }
                }
            }
            return bookmarks;
        }
        */

        /// <summary>
        /// 获取当前文档所有的数据源
        /// </summary>
        /// <returns>数据源列表</returns>
        public List<DataSource> GetSourceList()
        {
            return this.DocTemplateType.DataSourceList;
        }

        /// <summary>
        /// 合成母版和构健
        /// </summary>
        /// <param name="structureInfoList">构建列表</param>
        private void UnionDoc(List<IDocStructure> structureInfoList)
        {
            if (this.buildWord == null)
            {
                bool firstStructure = true;
                this.buildWord = new BuildWord();
                foreach (var structureInfoItem in structureInfoList)
                {
                    var stream = structureInfoItem.BuildDoc();
                    if (stream != null)
                    {
                        if (firstStructure)
                        {
                            this.buildWord.SetStream(stream);
                            this.buildWord.CreateBookmark("MergeDoc");
                            firstStructure = false;
                        }
                        else
                        {
                            this.buildWord.InsertDoc("MergeDoc", stream, structureInfoItem.NewSection);
                        }

                        //this.GetTextLabelValue(dict, structureInfoItem);
                    }
                }
            }
            else
            {
                this.buildWord.CreateBookmark("MergeDoc");
                foreach (var structureInfoItem in structureInfoList)
                {
                    this.buildWord.InsertDoc("MergeDoc", structureInfoItem.BuildDoc(), structureInfoItem.NewSection);
                    //this.GetTextLabelValue(dict, structureInfoItem);
                }
            }

            // 替换页眉页脚
            var dict = new Dictionary<string, string>();
            var inside = this.LabelList.Where(t => !t.RelateValue.Contains('@')).ToList();
            inside.ForEach(item =>
            {
                if (item is TextLabel && item.LabelName != null && !dict.ContainsKey(item.LabelName))
                {
                    var label = item as TextLabel;
                    dict.Add(label.LabelName, label.GetValue());
                }
            });
            this.buildWord.SetPageHeaderFooter(dict);
        }

        /// <summary>
        /// 获取所有文本标签值添加到参数Dict中
        /// </summary>
        /// <param name="dict">字典</param>
        /// <param name="structureInfoItem">构件信息项</param>
        private void GetTextLabelValue(Dictionary<string, string> dict, IDocStructure structureInfoItem)
        {
            if (structureInfoItem.LabelList != null)
            {
                foreach (var item in structureInfoItem.LabelList)
                {
                    if (item is TextLabel && item.LabelName != null && !dict.ContainsKey(item.LabelName))
                    {
                        var label = item as TextLabel;
                        dict.Add(label.LabelName, label.GetValue());
                    }
                }
            }
        }

        /// <summary>
        /// 构建文档
        /// </summary>
        /// <returns>文档流</returns>
        public Stream BuildDoc()
        {
            this.UnionDoc(this.StructureInfoList);
            this.buildWord.DelBookmarks();
            this.buildWord.UpdateFields();
            var docStream = this.buildWord.GetStream();
            docStream.Position = 0;
            return docStream;
        }

        /// <summary>
        /// 获取构建列表TextLabel集合
        /// </summary>
        /// <returns>构建列表TextLabel集合</returns>
        public List<TextLabelInfo> GetTextLabelList()
        {
            List<TextLabelInfo> labelList = new List<TextLabelInfo>();
            foreach (var structureInfoItem in this.StructureInfoList)
            {
                if (structureInfoItem is DocStructure)
                {
                    var tmpItem = structureInfoItem as DocStructure;
                    if (tmpItem.LabelList == null)
                    {
                        continue;
                    }

                    tmpItem.LabelList.ForEach(label =>
                    {
                        if (label is TextLabel)
                        {
                            var textLabel = label as TextLabel;
                            labelList.Add(new TextLabelInfo(
                                tmpItem.GetID, 
                                StructureType.Config, 
                                tmpItem.DocName, 
                                textLabel.LabelName, 
                                textLabel.FieldName,
                                textLabel.GetValue(),
                                textLabel.Relate, 
                                textLabel.FormatInfo, 
                                textLabel.DocControl,
                                textLabel.GetDataMethod, 
                                textLabel.DataSourceName, 
                                textLabel.ConfigValue, 
                                textLabel.FilterFieldName,
                                textLabel.FilterOperation, 
                                textLabel.FilterValue));
                        }
                        else if (label is ConditionLabel)
                        {
                            // var condLabel = label as ConditionLabel;
                            // var textLabel = condLabel.ConditionJudgment() as TextLabel;
                            // if (textLabel != null)
                            //    labelList.Add(new TextLabelInfo(tmpItem.GetID, StructureType.Config, tmpItem.DocName, textLabel.LabelName, textLabel.m_FieldName,
                            //        textLabel.GetValue(),
                            //        textLabel.Relate, textLabel.m_FormatInfo, textLabel.DocControl,
                            //        textLabel.m_GetDataMethod, textLabel.m_DataSourceName, textLabel.m_ConfigValue, textLabel.m_FilterFieldName,
                            //        textLabel.m_FilterOperation, textLabel.m_FilterValue));
                        }
                    });
                }
                else if (structureInfoItem is CustomDocStructure)
                {
                    var tmpItem = structureInfoItem as CustomDocStructure;
                    if (tmpItem.LabelList == null)
                    {
                        continue;
                    }

                    tmpItem.LabelList.ForEach(label =>
                    {
                        if (label is TextLabel)
                        {
                            var textLabel = label as TextLabel;
                            if (!string.IsNullOrEmpty(textLabel.LabelName))
                            {
                                labelList.Add(
                                    new TextLabelInfo(
                                        tmpItem.GetID, 
                                        StructureType.Custom,
                                        tmpItem.StructureName,
                                        textLabel.LabelName,
                                        textLabel.FieldName,
                                        textLabel.GetValue(),
                                        textLabel.Relate, 
                                        textLabel.FormatInfo, 
                                        textLabel.DocControl,
                                        textLabel.GetDataMethod, 
                                        textLabel.DataSourceName, 
                                        textLabel.ConfigValue,
                                        textLabel.FilterFieldName,
                                        textLabel.FilterOperation, 
                                        textLabel.FilterValue));
                            }
                        }
                    });
                }
            }

            return labelList;
        }

        /// <summary>
        /// 获取构建列表控件数据源集合
        /// </summary>
        /// <returns>数据源列表</returns>
        public Dictionary<string, JArray> GetDataSourceList()
        {
            Dictionary<string, JArray> dataSourceList = new Dictionary<string, JArray>();
            Dictionary<string, List<string>> tmpDS = new Dictionary<string, List<string>>();
            foreach (var structureInfoItem in this.StructureInfoList)
            {
                if (structureInfoItem is DocStructure)
                {
                    var tmpItem = structureInfoItem as DocStructure;
                    if (tmpItem.LabelList == null)
                    {
                        continue;
                    }

                    tmpItem.LabelList.ForEach(label =>
                    {
                        if (label is TextLabel)
                        {
                            TextLabel textLabel = label as TextLabel;
                            if (textLabel.GetDataMethod == GetDataMethod.Source && textLabel.DataSourceName.Trim() != string.Empty)
                            {
                                List<string> tmpFields = null;
                                if (tmpDS.ContainsKey(textLabel.DataSourceName))
                                {
                                    tmpFields = tmpDS[textLabel.DataSourceName];
                                }
                                else
                                {
                                    tmpFields = new List<string>();
                                    tmpDS.Add(textLabel.DataSourceName, tmpFields);
                                }

                                if (!tmpFields.Contains(textLabel.FilterFieldName))
                                {
                                    tmpFields.Add(textLabel.FilterFieldName);
                                }

                                if (!tmpFields.Contains(textLabel.FieldName))
                                {
                                    tmpFields.Add(textLabel.FieldName);
                                }
                            }

                            if (textLabel.DocControl is DropdownWithDataSourceControl)
                            {
                                var dropdownWithDataSourceControl = textLabel.DocControl as DropdownWithDataSourceControl;
                                List<string> tmpFields = null;
                                if (tmpDS.ContainsKey(dropdownWithDataSourceControl.DataSource))
                                {
                                    tmpFields = tmpDS[dropdownWithDataSourceControl.DataSource];
                                }
                                else
                                {
                                    tmpFields = new List<string>();
                                    tmpDS.Add(dropdownWithDataSourceControl.DataSource, tmpFields);
                                }

                                if (!tmpFields.Contains(dropdownWithDataSourceControl.TextField))
                                {
                                    tmpFields.Add(dropdownWithDataSourceControl.TextField);
                                }

                                if (!tmpFields.Contains(dropdownWithDataSourceControl.ValueField))
                                {
                                    tmpFields.Add(dropdownWithDataSourceControl.ValueField);
                                }

                                if (textLabel.Relate != null)
                                {
                                    foreach (var relateItem in textLabel.Relate)
                                    {
                                        if (!tmpFields.Contains(relateItem.FieldName))
                                        {
                                            tmpFields.Add(relateItem.FieldName);
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }

            foreach (var item in tmpDS)
            {
                var dataSource = this.DocTemplateType.DataSourceList.Find(it => it.DataSourceName.Equals(item.Key));
                JArray newJArray = new JArray();
                if (dataSource != null)
                {
                    DataTable dt = dataSource.GetDataTable();
                    if (dt != null)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            JObject newJObject = new JObject();
                            foreach (var field in item.Value)
                            {
                                string newField = field.Replace("@", string.Empty);
                                if (field.Trim() == string.Empty || newJObject.Property(newField) != null)
                                {
                                    continue;
                                }

                                // if (field.Trim() == "" || newJObject.ToString().IndexOf("\"" + newField + "\":")>=0)
                                newJObject.Add(new JProperty(newField, dr[newField].ToString()));
                            }

                            newJArray.Add(newJObject);
                        }

                        dataSourceList.Add(item.Key, newJArray);
                    }
                }
            }

            return dataSourceList;
        }
    } // end CompanyTemplate

    /// <summary>
    /// 构件项
    /// </summary>
    public class Structure
    {
        /// <summary>
        /// 构件类型
        /// </summary>
        public StructureType StructureType
        {
            get;
            set;
        }

        /// <summary>
        /// 构件名称
        /// </summary>
        public string StructureName
        {
            get;
            set;
        }

        /// <summary>
        /// 主键
        /// </summary>
        public decimal Key
        {
            get;
            set;
        }

        /// <summary>
        /// 条件
        /// </summary>
        public string Condition
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 条件项
    /// </summary>
    public class Condition
    {
        /// <summary>
        /// 数据源
        /// </summary>
        public string DataSourceName
        {
            get;
            set;
        }

        /// <summary>
        /// 数据标签名称
        /// </summary>
        public string DataLabelName
        {
            get;
            set;
        }

        /// <summary>
        /// 操作符
        /// </summary>
        public string Operation
        {
            get;
            set;
        }

        /// <summary>
        /// 值
        /// </summary>
        public string Value
        {
            get;
            set;
        }
    }
} // end namespace