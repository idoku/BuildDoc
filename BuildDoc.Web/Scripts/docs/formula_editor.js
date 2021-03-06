﻿/****
[名称] 公式编辑器
[描述] formual editor
[作者] doku
[时间] 2015.03.18   
*****/

 
    function formualEditor(operands) {        
        this.operands = operands;
        this.formual = "";
        this.container = arguments[1] || "dformual";
        this.init();
        if (operands.length > 1) { //test use 1
            this.enableautocomplete();
        }    

        //扩展方法
        $.fn.extend({
            insertAtCaret: function (myValue) {
                var $t = $(this)[0];
                if (document.selection) {
                    this.focus();
                    sel = document.selection.createRange();
                    sel.text = myValue;
                    this.focus();
                } else
                    if ($t.selectionStart || $t.selectionStart == '0') {
                        var startPos = $t.selectionStart;
                        var endPos = $t.selectionEnd;
                        var scrollTop = $t.scrollTop;
                        $t.value = $t.value.substring(0, startPos) + myValue + $t.value.substring(endPos, $t.value.length);
                        this.focus();
                        $t.selectionStart = startPos + myValue.length;
                        $t.selectionEnd = startPos + myValue.length;
                        $t.scrollTop = scrollTop;
                    } else {
                        this.value += myValue;
                        this.focus();
                    }
            }
        });


    }


    //编辑器初始化
    formualEditor.prototype.init = function () {
        $('#formual_operands').empty();
        var self = this;
        $.each(this.operands, function (i, operaton) {
            $('#formual_operands').append("<li>" + operaton + "</li>");
        });
        //初始事件
        this.event();
    }

    //初始化事件
    formualEditor.prototype.event = function () {
        //选择分量
        $('#formual_operands li').unbind('click');
        $('#formual_operands li').click(function () {
            $('#txtOperand').val($(this).text());
        });

        //选择操作
        $('#btnAddOperand').unbind('click');
        $('#btnAddOperand').click(function () {
            if ($.trim($("#txtOperand").val()) != "") {
                $('#txtFormula').insertAtCaret($('#txtOperand').val());
                $('#txtOperand').val("");

            }
        });    

        //选择运算符
        $('.formula_operator input').unbind('click');
        $('.formula_operator input').click(function () {
            $('#txtFormula').insertAtCaret($(this).val() + " ");
        });
    
       //
    }

    //返回公式值
    formualEditor.prototype.save = function () {
        return $('#txtFormula').val();
    };

    

    //自动查询开启
    formualEditor.prototype.enableautocomplete = function () {               
        $('#txtOperand').autocomplete({
            minLength: 0,
            source: this.operands
        }).click(function () {
            $(this).autocomplete("search", $(this).val());
        });
    }
 

