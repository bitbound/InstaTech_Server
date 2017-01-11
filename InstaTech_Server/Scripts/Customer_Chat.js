function initCustomerChat() {
    var request = {
        "Type": "GetSupportCategories"
    };
    InstaTech.Socket_Chat.send(JSON.stringify(request));
    var request = {
        "Type": "GetCustomerFormInfo"
    };
    InstaTech.Socket_Chat.send(JSON.stringify(request));
    $("a.tooltip-anchor").button({
        icon: "ui-icon-help",
        showLabel: false
    });
    $("a.tooltip-anchor").tooltip();
}
function submitCustomerLogin(e) {
    e.preventDefault();
    var formData = {};
    $("#formCustomerChat").find("input,select,textarea").each(function (index, item) {
        formData[item.name] = item.value;
    });
    InstaTech.Socket_Chat.send(JSON.stringify(formData));
}
function changeSupportCategory(e) {
    var category = $("#selectSupportCategory").val();
    if (category == "Other") {
        $("#selectSupportType").parent().parent().hide();
        $("#selectSupportType").removeAttr("required");
    }
    else {
        $("#selectSupportType").attr("required", true);
        $("#selectSupportType").parent().parent().show();
        var request = {
            "Type": "GetSupportTypes",
            "SupportCategory": category
        };
        InstaTech.Socket_Chat.send(JSON.stringify(request));
    }
}
function submitCustomerMessage(e) {
    if ($("#textCustomerInput").val() == "") {
        return;
    }
    var message = $("#textCustomerInput").val().replace("\n", "<br/>");
    $("#textCustomerInput").val("");
    var divMessage = document.createElement("div");
    divMessage.classList.add("sent-chat");
    divMessage.innerHTML = '<div class="arrow-right"></div><div class="chat-message-header">You at ' + new Date().toLocaleTimeString() + "</div>" + message;
    $("#divCustomerMessages").append(divMessage);
    $("#divCustomerMessages")[0].scrollTop = $("#divCustomerMessages")[0].scrollHeight;
    var encoded = btoa(message);
    var request = {
        "Type": "ChatMessage",
        "Message": encoded
    };
    InstaTech.Socket_Chat.send(JSON.stringify(request));
}
function customerKeyDown(e) {
    if (e.key.toLowerCase() == "enter" && e.shiftKey == false) {
        e.preventDefault();
        $("#buttonCustomerSend").click();
    }
    ;
    var request = {
        "Type": "Typing"
    };
    InstaTech.Socket_Chat.send(JSON.stringify(request));
}
function closeCustomerSession(e) {
    var dialog = document.createElement("div");
    dialog.innerHTML = "Are you sure you want to close your session?";
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: "Confirm Session Closure",
        classes: { "ui-dialog-title": "center-aligned" },
        buttons: [
            {
                text: "Close",
                click: function () {
                    var request = {
                        "Type": "SessionEnded",
                    };
                    InstaTech.Socket_Chat.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            },
            {
                text: "Cancel",
                click: function () {
                    $(this).dialog("close");
                }
            }
        ],
        close: function () {
            $(this).dialog('destroy').remove();
        }
    });
}
function dragOverCustomerChat(e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = "copy";
}
;
function dropOnCustomerChat(e) {
    e.preventDefault();
    if (e.dataTransfer.files.length < 1) {
        return;
    }
    if (InstaTech.Socket_Chat.readyState != WebSocket.OPEN) {
        return;
    }
    transferFileCustomer(e.dataTransfer.files);
}
;
function transferFileCustomer(e) {
    for (var i = 0; i < e.length; i++) {
        var file = e[i];
        var strPath = "/Services/File_Transfer_Chat.cshtml";
        var fd = new FormData();
        fd.append('fileUpload', file);
        var xhr = new XMLHttpRequest();
        xhr.open('POST', strPath, true);
        xhr.onload = function () {
            if (xhr.status === 200) {
                var fileName = xhr.responseText;
                var url = location.href + "Services/File_Transfer_Chat.cshtml?file=" + fileName;
                $("#textCustomerInput").val('File Sharing Link: <a target="_blank" href="' + url + '">' + fileName + '</a>');
                submitCustomerMessage();
            }
            else {
                showDialog("Upload Failed", "File upload failed.");
            }
        };
        xhr.onprogress = function (e) {
            $("#divCustomerStatus").html("File Upload: " + Math.round(e.loaded / e.total * 100) + "%");
        };
        xhr.send(fd);
    }
}
