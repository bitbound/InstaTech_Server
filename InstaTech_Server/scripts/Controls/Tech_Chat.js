function initTechChat() {
    if (InstaTech.UserID != undefined && InstaTech.AuthenticationToken != undefined) {
        $("#inputTechChatUserID").val(InstaTech.UserID);
        $("#inputTechChatUserID").attr("disabled", true);
        $("#inputTechChatPassword").val("**********");
        $("#inputTechChatPassword").attr("disabled", true);
        if (localStorage["RememberMe"])
        {
            document.getElementById("inputTechChatRememberMe").checked = true;
        }
    }
}
function submitTechChatLogin(e) {
    e.preventDefault();
    var formData = {};
    $("#formTechChatLogin").find("input").each(function (index, item) {
        if (item.type == "checkbox")
        {
            formData[item.name] = item.checked;
        }
        else
        {
            formData[item.name] = item.value;
        }
    });
    if (InstaTech.AuthenticationToken) 
    {
        formData["AuthenticationToken"] = InstaTech.AuthenticationToken;
    }
    InstaTech.Socket_Main.send(JSON.stringify(formData));
}
function forgotPasswordChat(e) {
    if ($("#inputTechChatUserID").val().length == 0) {
        showDialog("User ID Required", "You must first enter a user ID into the form before you can reset the password.");
        return;
    }
    var dialog = document.createElement("div");
    dialog.innerHTML = "This will reset your password and send a temporary password to your email.<br/><br/>Proceed?";
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: "Confirm Password Reset",
        classes: { "ui-dialog-title": "center-aligned" },
        buttons: [
            {
                text: "Yes",
                click: function () {
                    var request = {
                        "Type": "ForgotPassword",
                        "UserID": $("#inputTechChatUserID").val()
                    };
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            },
            {
                text: "No",
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
function queueBlockClicked(e) {
    if ($(e.currentTarget).hasClass("selected")) {
        return;
    }
    var request = {
        "Type": "GetCases",
        "AuthenticationToken": InstaTech.AuthenticationToken
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
    $(".queue-block").removeClass("selected");
    $(".queue-block .arrow-right").remove();
    $(e.currentTarget).addClass("selected");
    var arrowRight = document.createElement("div");
    arrowRight.classList.add("arrow-right");
    $(e.currentTarget).append(arrowRight);
    $("#divChatBoxTech").slideUp();
    $("#divTechMessages").html("");
    $(".col3").height(0);
    $(".col3").animate({ "width": 0 });
    $(".col2").animate({ "width": 0 }, function () {
        $(".col2 .queue-list").html("");
        var filteredCases;
        if ($(e.currentTarget).attr("queue") == "All") {
            filteredCases = InstaTech.Cases;
        }
        else {
            filteredCases = InstaTech.Cases.filter(function (value, index) {
                return value.SupportQueue == $(e.currentTarget).attr("queue");
            });
        }
        filteredCases.sort(function (a, b) {
            if (parseNETDate(a.DTCreated) < parseNETDate(b.DTCreated)) {
                return -1;
            }
            else if (parseNETDate(b.DTCreated) < parseNETDate(a.DTCreated)) {
                return 1;
            }
            else {
                return 0;
            }
        });
        for (var i = 0; i < filteredCases.length; i++) {
            addCaseBlock(filteredCases[i]);
        }
        $(".col2").animate({ "width": "200px" });
    });
}
function caseBlockClicked(e) {
    $(".col2 .queue-list .case-block").removeClass("selected");
    $(".col2 .queue-list .case-block .arrow-right").remove();
    e.currentTarget.classList.add("selected");
    var arrowRight = document.createElement("div");
    arrowRight.classList.add("arrow-right");
    $(e.currentTarget).append(arrowRight);
    $("#divChatBoxTech").slideUp();
    $("#divTechMessages").html("");
    $(".col3").animate({ "width": 0 }, function () {
        var caseInfo = InstaTech.Cases.filter(function (item) { return item.CaseID == e.currentTarget.getAttribute("case-id"); })[0];
        $("#inputTechQueueCaseID").val(caseInfo.CaseID);
        $("#inputTechQueueFirstName").val(caseInfo.CustomerFirstName);
        $("#inputTechQueueLastName").val(caseInfo.CustomerLastName);
        $("#inputTechQueueUserID").val(caseInfo.CustomerUserID);
        $("#inputTechQueueComputerName").val(caseInfo.CustomerComputerName);
        $("#inputTechQueuePhone").val(caseInfo.CustomerPhone);
        $("#inputTechQueueEmail").val(caseInfo.CustomerEmail);
        $("#inputTechQueueCategory").val(caseInfo.SupportCategory);
        $("#inputTechQueueType").val(caseInfo.SupportType);
        $("#textTechQueueDetails").val(caseInfo.Details);
        $(".col3").height("");
        $(".col3").animate({ "width": "500px" }, function () {
            window.scroll(0, $(".col3").offset().top);
        });
    });
}
function addCaseBlock(objCase) {
    var caseBlock = document.createElement("div");
    caseBlock.id = "divCase" + objCase.CaseID;
    caseBlock.classList.add("case-block");
    if (objCase.Locked) {
        caseBlock.classList.add("case-locked");
    }
    caseBlock.innerHTML = objCase.CustomerFirstName + ' ' + objCase.CustomerLastName + '<br/><span class="case-block-wait">Wait Time: <span class="wait-time">' + getTimeSince(objCase.DTCreated) + '</span></span><br/><hr/><span class="case-block-category">' + objCase.SupportCategory + '</span> - <span class="case-block-type">' + objCase.SupportType + '</span>';
    caseBlock.setAttribute("case-id", objCase.CaseID);
    $(caseBlock).click(caseBlockClicked);
    $(".col2 .queue-list").append(caseBlock);
}
function takeCase(e) {
    var caseID = $("#inputTechQueueCaseID").val();
    if ($.isEmptyObject(caseID)) {
        sendClientError("Case ID is empty in takeCase().");
        return;
    }
    var request = {
        "Type": "TakeCase",
        "CaseID": caseID,
        "AuthenticationToken": InstaTech.AuthenticationToken
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
function techKeyDown(e) {
    if (e.key.toLowerCase() == "enter" && e.shiftKey == false) {
        e.preventDefault();
        $("#buttonTechSend").click();
    }
    ;
    var request = {
        "AuthenticationToken": InstaTech.AuthenticationToken,
        "Type": "Typing"
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
function submitTechMessage(e) {
    if ($("#textTechInput").val() == "") {
        return;
    }
    var message = $("#textTechInput").val().replace("\n", "<br/>");
    $("#textTechInput").val("");
    var divMessage = document.createElement("div");
    divMessage.classList.add("sent-chat");
    divMessage.innerHTML = '<div class="arrow-right"></div><div class="chat-message-header">You at ' + new Date().toLocaleTimeString() + "</div>" + message;
    $("#divTechMessages").append(divMessage);
    $("#divTechMessages")[0].scrollTop = $("#divTechMessages")[0].scrollHeight;
    var encoded = btoa(message);
    var request = {
        "AuthenticationToken": InstaTech.AuthenticationToken,
        "Type": "ChatMessage",
        "Message": encoded
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
function updateQueueVolumes(e) {
    if (InstaTech.Socket_Main.readyState != 1) {
        window.clearInterval(InstaTech.QueueWaitTimer);
        return;
    }
    $("#divQueueAll .queue-volume").html(InstaTech.Cases.length);
    $(".queue-block").each(function (index, elem) {
        if (elem.getAttribute("queue") != "All") {
            $("#" + elem.id + " .queue-volume").html(InstaTech.Cases.filter(function (value, ind) {
                return value.SupportQueue == elem.getAttribute("queue");
            }).length);
        }
    });
    $(".case-block-wait .wait-time").each(function (index, elem) {
        var caseID = $(elem).parent().parent().attr("id").replace("divCase", "");
        var created = InstaTech.Cases.filter(function (value, index) {
            return value.CaseID == caseID;
        })[0].DTCreated;
        elem.innerHTML = getTimeSince(created);
    });
}
function closeTechSession(e) {
    var dialog = document.createElement("div");
    dialog.innerHTML = "Are you sure you want to close this session as resolved?";
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
                        "Details": "Thank you for contacting us!"
                    };
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                    $("#divChatBoxTech").slideUp();
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
function dragOverTechChat(e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = "copy";
}
;
function dropOnTechChat(e) {
    e.preventDefault();
    if (e.dataTransfer.files.length < 1) {
        return;
    }
    if (InstaTech.Socket_Main.readyState != WebSocket.OPEN) {
        return;
    }
    transferFileTech(e.dataTransfer.files);
}
;
function transferFileTech(e) {
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
                $("#textTechInput").val('File Sharing Link: <a target="_blank" href="' + url + '">' + fileName + '</a>');
                submitTechMessage();
            }
            else {
                showDialog("Upload Failed", "File upload failed.");
            }
        };
        xhr.onprogress = function (e) {
            $("#divTechStatus").html("File Upload: " + Math.round(e.loaded / e.total * 100) + "%");
        };
        xhr.send(fd);
    }
}
function sendToQueue(e) {
    var request = {
        "Type": "SendToQueue",
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
    $("#divChatBoxTech").slideUp();
    $(".col3").animate({ "width": 0 });
}
function moveCase(e) {
    if ($("#divChatBoxTech").is(":visible")) {
        return;
    }
    var dialog = document.createElement("div");
    var divInnerDial = document.createElement("div");
    divInnerDial.style.textAlign = "center";
    divInnerDial.innerHTML = "<div>Select the new category and type for this case.</div><br/>";
    var selectCategory = document.createElement("select");
    selectCategory.id = "selectMoveCaseCategory";
    var selectType = document.createElement("select");
    selectType.id = "selectMoveCaseType";
    divInnerDial.appendChild(selectCategory);
    divInnerDial.innerHTML += "<br/>";
    divInnerDial.appendChild(selectType);
    dialog.appendChild(divInnerDial);
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: "Case Transfer",
        classes: { "ui-dialog-title": "center-aligned" },
        buttons: [
            {
                text: "Transfer",
                click: function () {
                    if ($("#selectMoveCaseCategory").val() != "Other") {
                        if (!$("#selectMoveCaseCategory").val() || !$("#selectMoveCaseType").val()) {
                            showDialog("Selections Required", "You must select a category and type.  If category is Other, type may be ommitted.");
                            return;
                        }
                    }
                    var request = {
                        "Type": "CaseUpdate",
                        "CaseID": $("#inputTechQueueCaseID").val(),
                        "Status": "Transfer",
                        "SupportCategory": $("#selectMoveCaseCategory").val(),
                        "SupportType": $("#selectMoveCaseType").val()
                    };
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                    $("#divChatBoxTech").slideUp();
                }
            },
            {
                text: "Cancel",
                click: function () {
                    $(this).dialog("close");
                }
            }
        ],
        open: function () {
            $(this).find("select").selectmenu();
            InstaTech.Socket_Main.send(JSON.stringify({ "Type": "GetSupportCategories" }));
            $("#selectMoveCaseCategory").on("selectmenuchange", function (e) {
                var category = $("#selectMoveCaseCategory").val();
                if (category == "Other") {
                    $("#selectMoveCaseType").selectmenu("disable");
                }
                else {
                    $("#selectMoveCaseType").selectmenu("enable");
                    var request = {
                        "Type": "GetSupportTypes",
                        "SupportCategory": category
                    };
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                }
            });
        },
        close: function () {
            $(this).dialog('destroy').remove();
        }
    });
}
function lockCase(e) {
    var request = {
        "Type": "LockCase",
        "CaseID": $("#inputTechQueueCaseID").val(),
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
