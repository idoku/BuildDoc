﻿var document = new Object();
document.remarks = new Array();
document.datasource = new Array();


$(function () {

    document.GetRemarks();

    $('[name="GetDataMethod"]').hide();

    //数据源切换
    $('#DataMethod').change(function () {
        var val = $(this).val();
        $('[name="GetDataMethod"][method="' + val + '"]').show();
        $('[name="GetDataMethod"][method!="' + val + '"]').hide();
    });
    $('#DataMethod').change();

    //数据类型
    $('#FormatType').change(function () {
        var val = $(this).val();
        $('[name="FormatInfo"][format!="' + val + '"]').hide();
        $('[name="FormatInfo"][format*="' + val + '"]').show();
    });
    $('#FormatType').change();

    //控件类型
    $('#ControlType').change(function () {
        var val = $(this).val();
        $('[name="LabelControl"][control="' + val + '"]').show();
        $('[name="LabelControl"][control!="' + val + '"]').hide();
        $('#FillType').change();
    });
    $('#ControlType').change();

    //填充方式
    $('#FillType').change(function () {
        var val = $(this).val();
        var control = $('#ControlType').val();
        //屏蔽填充方式不一致的选项
        $('[name="LabelFill"][control!="' + control + '"]').hide();
        $('[name="LabelFill"][control="' + control + '"][fill="' + val + '"]').show();
        $('[name="LabelFill"][control="' + control + '"][fill!="' + val + '"]').hide();

    });
    $('#FillType').change();

    //设置公式
    $('#txtLabelFormula').click(function () {
        AlertDiv('#alert_Formula');
        var fields = [];

        $('#grid').find("td[title=\"labelName\"]").each(function () {
            fields.push($(this).text());
        })
        var feditor = new formualEditor(fields);
    });

    //条件条件
    $('#btnAddCondition').click(function () {
        document.AddConditionRow(0, null);
    });

    //设置条件公式
    $('#btnAddConditionSet').click(function () {
        document.AddConditionSetExpRow("", "");
    });
    
    //保存条件公式
    $('#btnConditionSetSave').click(function () {
        document.SaveConditionSet();
    });


    document.BindDataSource($('#sltDataSource'));
});

//获取书签
document.GetRemarks = function () {
    $.ajax({
        url: "GetRemarks",
        type: "post",
        dataType:"json",
        success: function (datas) {
            document.remarks = new Array();
            if (datas) {
             
                $.each(datas, function (i, remark) {
                    var bookInfo = [];
                    bookInfo.LabelName = remark;
                    bookInfo.Name = remark;
                    bookInfo.LabelType = "TextLabel";
                    document.AddRemarkRow(i + 1, bookInfo);
                    document.remarks.push(bookInfo);
                });

            }
        }
    });
}

//解析json配置--为表格增加标签行
document.AddRemarkRow = function (num,remark) {
    var mould = $('#docMould').find("[name='remarkMould']").clone();
    mould.css("display", "");
    //行赋值
    mould.find("[title='number']").text(num);
    mould.find("[title='labelName']").text(remark.LabelName);
    mould.find("[title='labelType']").text(remark.LabelType);    

    $('#remarks tr:last').after(mould);

}

//添加条件行
document.AddConditionRow = function (num, condition) {
    var mould = $('#docMould').find("[name='conditionMould']").clone();
    mould.css("display", "");
    //行赋值
    if (num <= 0) {
        num = $('#conditions tr').length;
        condition = [];
        condition.ConditionStr = "";
        condition.LabelType = "";
    }
    mould.find("[title='number']").text(num);
    mould.find("[title='conditionStr']").html(condition.ConditionStr).click(function () {
        document.AlertCondition(condition.ConditionStr);
    });

    mould.find("[title='conditionConfig']").text(condition.LabelType).click(function () {
        document.AlertCondition(condition.ConditionStr);
    });

    $('#conditions tr:last').after(mould);
}

//添加条件表达式行
document.AddConditionSetExpRow = function (conditionSetExp, expLogic) {
    var mould = $('#docMould').find("[name='conditionSetExpMould']").clone();
    mould.css("display", "");

    //条件大于1，显示逻辑
    if ($('#condtionSetExps tr:visible').length > 0) { 
        mould.find("[name='sltConditionSetJoin']").show();
    }

    //行赋值
    if (conditionSetExp != "") {
        //表达式解析 
        //方法1：逐条解析
        //方法2：空格解析
        //方法3：先解析运算符，再解析分量
        var reg = /[>,<,==,>=,<=]/;
        var operate = conditionSetExp.match(reg);
        //运算符
        mould.find("[name='sltConditionSetOperate']").val(operate);
        //运算分量
        var expArray = conditionSetExp.split(reg);
        mould.find("[name='txtConditionSetRemark']").val(expArray[0]);
        mould.find("[name='txtConditionSetValue']").val(expArray[1]);
        //逻辑分量
        mould.find("[name='sltConditionSetJoin']").val(expLogic);
    }

    $('#condtionSetExps').append(mould);

}

//保存条件公式
document.SaveConditionSet = function () {

}

document.BindDataSource = function (sltSource) {
    $.getJSON("/DataSource/GetDataSource", function (datas) {
        document.datasource = datas;
        //绑定数据源
        for (var i = 0; i < datas.length; i++) {
            $('<option>', {
                val: datas[i].DATA_SOURCE_NAME,
                text: datas[i].DATA_SOURCE_NAME
            }).appendTo(sltSource);
        };
        //取数据源字段
        //
        $(sltSource).change(function () {
            var index = $(this).prop('selectedIndex');
            var fields = datas[index].Fields;
            document.BindFields($('#sltDisplayFields'), fields);
            document.BindFields($('#sltFilterFields'), fields);
            document.BindFields($('#sltFillDataSource'), fields);
            document.BindFields($('#sltFormatFields'), fields);
        });
        $(sltSource).change();
    });
};

//筛选数据源
document.BindFields = function (sltFields, fields) {
    if (fields != null) {
        for (var i = 0; i < fields.length; i++) {
            $('<option>', {
                val: fields[i],
                text: fields[i]
            }).appendTo($(sltFields));
        }
    }
}



//弹出设置条件等式的界面
document.AlertCondition = function (conditionStr) {
    AlertDiv('#alert_ConditionSet');
    var fields = new Array("估价目的", "估价对象类型");
    $.each(document.remarks, function (i, remark) {
        fields.push(remark.LabelName);
    });
    //解析条件表达式
    var reg = /[&&,||]/;    
    var conditions = conditionStr.split(reg);
    $(conditions, function (i, conditionExp) {
        //新增行
        document.AddConditionExpRow(conditionExp,reg.source);
    })
    
}