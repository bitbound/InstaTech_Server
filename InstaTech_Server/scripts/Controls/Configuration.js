function initConfiguration() {
    $("a.tooltip-anchor").button({
        icon: "ui-icon-help",
        showLabel: false
    });
    $("a.tooltip-anchor").tooltip();
    var request = {
        "Type": "GetConfiguration",
        "AuthenticationToken": InstaTech.AuthenticationToken
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
    request = {
        "Type": "GetSupportCategories",
        "ElementID": "selectSupportCategories"
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}

function toggleProperty(e) {
    var request = {
        "Type": "SetConfigProperty",
        "Property": e.currentTarget.getAttribute("prop"),
        "AuthenticationToken": InstaTech.AuthenticationToken
    }
    if ($(e.currentTarget).attr("on") == "false") {
        $(e.currentTarget).attr("on", "true");
        request.Value = true;
    }
    else {
        $(e.currentTarget).attr("on", "false");
        request.Value = false;
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
    showTooltip(e.currentTarget, "right", "green", "Saved.");
}

function changeStringProperty(e) {
    var request = {
        "Type": "SetConfigProperty",
        "Property": e.currentTarget.getAttribute("prop"),
        "AuthenticationToken": InstaTech.AuthenticationToken,
        "Value": e.currentTarget.value
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
    showTooltip(e.currentTarget, "right", "green", "Saved.");
}
function changeConfigSupportCategory(e) {
    $("#selectSupportQueues").attr("disabled", true);
    var category = $("#selectSupportCategories").val();
    if (category == "Other") {
        $("#selectSupportTypes").parent().parent().hide();
        $("#selectSupportCategories").siblings("img").hide();
        $("#selectSupportQueues").siblings("img").hide();
        $("#selectSupportQueues").val("Other");
        $("#inputSupportQueue").val("Other");
    }
    else {
        $("#selectSupportTypes").parent().parent().show();
        $("#selectSupportCategories").siblings("img").show();
        $("#selectSupportQueues").siblings("img").show();
        var request = {
            "Type": "GetSupportTypes",
            "SupportCategory": category,
            "ElementID": "selectSupportTypes"
        };
        InstaTech.Socket_Main.send(JSON.stringify(request));
        $("#selectSupportQueues")[0].selectedIndex = -1;
    }
}

function changeConfigSupportType(e) {
    if (e.currentTarget.value == "") {
        $("#selectSupportQueues").attr("disabled", true);
        return;
    }
    $("#selectSupportQueues").attr("disabled", false);
    var request = {
        "Type": "GetSupportQueue",
        "SupportCategory": $("#selectSupportCategories").val(),
        "SupportType": $("#selectSupportTypes").val(),
        "ElementID": "selectSupportQueues"
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}

function changeConfigSupportQueue(e) {
    var request = {
        "Type": "SetSupportQueue",
        "SupportCategory": $("#selectSupportCategories").val(),
        "SupportType": $("#selectSupportTypes").val(),
        "SupportQueue": $("#selectSupportQueues").val(),
        "AuthenticationToken": InstaTech.AuthenticationToken
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
function addSupportCategory(e) {
    var buttons = [
        {
            text: "OK",
            click: function() {
                if ($("#inputNewCategory").val().length < 3) {
                    showDialog("Invalid Length", "The category name must be at least 3 characters long.");
                    return;
                }
                else {
                    var request = {
                        "Type": "AddSupportCategory",
                        "SupportCategory": $("#inputNewCategory").val(),
                        "AuthenticationToken": InstaTech.AuthenticationToken
                    }
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            }
        },
        {
            text: "Cancel",
            click: function() {
                $(this).dialog("close");
            }
        }
    ];
    showDialogEx("New Support Category", "What will be the name of the new category?<br/><br/><input id='inputNewCategory' style='width:80%'/>", buttons)
}
function addSupportType(e) {
    if (!$("#selectSupportCategories").val()) {
        return;
    }
    var buttons = [
        {
            text: "OK",
            click: function() {
                if ($("#inputNewType").val().length < 3) {
                    showDialog("Invalid Length", "The type name must be at least 3 characters long.");
                    return;
                }
                else {
                    var request = {
                        "Type": "AddSupportType",
                        "SupportCategory": $("#selectSupportCategories").val(),
                        "SupportType": $("#inputNewType").val(),
                        "AuthenticationToken": InstaTech.AuthenticationToken
                    }
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            }
        },
        {
            text: "Cancel",
            click: function() {
                $(this).dialog("close");
            }
        }
    ];
    showDialogEx("New Support Type", "What will be the name of the new type?<br/><br/><input id='inputNewType' style='width:80%'/>", buttons)
}
function addSupportQueue(e) {
    if (!$("#selectSupportCategories").val() || !$("#selectSupportTypes").val()) {
        showDialog("Category/Type Required", "You must have a category and type selected before you can add a queue.");
        return;
    }
    var buttons = [
        {
            text: "OK",
            click: function() {
                if ($("#inputNewQueue").val().length < 3) {
                    showDialog("Invalid Length", "The queue name must be at least 3 characters long.");
                    return;
                }
                else {
                    var request = {
                        "Type": "AddSupportQueue",
                        "SupportCategory": $("#selectSupportCategories").val(),
                        "SupportType": $("#selectSupportTypes").val(),
                        "SupportQueue": $("#inputNewQueue").val(),
                        "AuthenticationToken": InstaTech.AuthenticationToken
                    };
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            }
        },
        {
            text: "Cancel",
            click: function() {
                $(this).dialog("close");
            }
        }
    ];
    showDialogEx("New Support Type", "What will be the name of the new type?<br/><br/><input id='inputNewQueue' style='width:80%'/>", buttons)
}
function deleteSupportCategory(e) {
    if (!$("#selectSupportCategories").val()) {
        return;
    }
    if ($("#selectSupportCategories").val() == "Other") {
        showDialog("Unable to Delete", "You cannot delete the Other category.");
        return;
    }
    var buttons = [
        {
            text: "OK",
            click: function() {
                    var request = {
                        "Type": "DeleteSupportCategory",
                        "SupportCategory": $("#selectSupportCategories").val(),
                        "AuthenticationToken": InstaTech.AuthenticationToken
                    }
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
            }
        },
        {
            text: "Cancel",
            click: function() {
                $(this).dialog("close");
            }
        }
    ];
    showDialogEx("Confirm Deletion", "This will delete the category and all its types.  Proceed?", buttons);
}
function deleteSupportType(e) {
    if (!$("#selectSupportTypes").val()) {
        return;
    }
    if ($("#selectSupportTypes").children().length == 1) {
        showDialog("Unable to Delete", "You cannot delete the last type in a category.  Delete the category instead.");
        return;
    }
    var buttons = [
        {
            text: "OK",
            click: function() {
                var request = {
                    "Type": "DeleteSupportType",
                    "SupportCategory": $("#selectSupportCategories").val(),
                    "SupportType": $("#selectSupportTypes").val(),
                    "AuthenticationToken": InstaTech.AuthenticationToken
                }
                InstaTech.Socket_Main.send(JSON.stringify(request));
                $(this).dialog("close");
            }
        },
        {
            text: "Cancel",
            click: function() {
                $(this).dialog("close");
            }
        }
    ];
    showDialogEx("Confirm Deletion", "This will delete the selected type.  Proceed?", buttons);
}
function deleteSupportQueue(e) {
    showDialog("Queue Deletion", "You cannot delete a queue directly.  You must delete all category/type combinations that are in this queue or reassign them to a different queue.  The queue will not exist after that.");
}
function addComputerGroup() {
    var buttons = [
        {
            text: "OK",
            click: function() {
                if ($("#inputNewGroup").val().length < 3) {
                    showDialog("Invalid Length", "The group name must be at least 3 characters long.");
                    return;
                }
                else {
                    var request = {
                        "Type": "AddComputerGroup",
                        "Group": $("#inputNewGroup").val(),
                        "AuthenticationToken": InstaTech.AuthenticationToken
                    };
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            }
        },
        {
            text: "Cancel",
            click: function() {
                $(this).dialog("close");
            }
        }
    ];
    showDialogEx("New Computer Group", "What will be the name of the new group?<br/><br/><input id='inputNewGroup' style='width:80%'/>", buttons)
}
function deleteComputerGroup() {
    var buttons = [
        {
            text: "OK",
            click: function() {
                var request = {
                    "Type": "DeleteComputerGroup",
                    "Group": $("#selectComputerGroups").val(),
                    "AuthenticationToken": InstaTech.AuthenticationToken
                }
                InstaTech.Socket_Main.send(JSON.stringify(request));
                $(this).dialog("close");
            }
        },
        {
            text: "Cancel",
            click: function() {
                $(this).dialog("close");
            }
        }
    ];
    showDialogEx("Confirm Deletion", "This will delete the selected group.  Proceed?", buttons);
}