function handleTechChatLogin(e) {
    if (e.Status == "new required") {
        $("#inputTechConfirmNewPassword, #inputTechNewPassword").attr("required", true);
        $("#inputTechConfirmNewPassword, #inputTechNewPassword").parent("td").parent("tr").show();
        return;
    }
    else if (e.Status == "ok") {
        clearCachedCreds();
        InstaTech.Context = "Technician";
        InstaTech.UserID = $("#inputTechChatUserID").val();
        InstaTech.AuthenticationToken = e.AuthenticationToken;
        if (document.getElementById("inputTechChatRememberMe").checked) {
            localStorage["RememberMe"] = true;
            localStorage["UserID"] = InstaTech.UserID;
            localStorage["AuthenticationToken"] = InstaTech.AuthenticationToken;
        }
        setMainLoginFrame();
        $("#divTechChatLoginFrame").fadeOut(750, function () {
            $("#divQueueFrame").fadeIn(750, function () {
                $("#divTechChat .portal-content-frame").animate({
                    "width": "90vw"
                }, function () {
                    window.scroll(0, document.getElementById("divQueueFrame").offsetTop);
                });
                $(".queue-block").off("click").on("click", queueBlockClicked);
                var request = {
                    "Type": "GetQueues",
                    "AuthenticationToken": InstaTech.AuthenticationToken
                };
                InstaTech.Socket_Main.send(JSON.stringify(request));
            });
        });
        if (InstaTech.QueueWaitTimer) {
            window.clearInterval(InstaTech.QueueWaitTimer);
        }
        InstaTech.QueueWaitTimer = window.setInterval(updateQueueVolumes, 2000);
    }
    else if (e.Status == "invalid") {
        clearCachedCreds();
        showDialog("Incorrect Credentials", "The user ID or password is incorrect.  Please try again.");
        return;
    }
    else if (e.Status == "locked") {
        showDialog("Account Locked", "Your account as been locked due to failed login attempts.  It will unlock automatically after 10 minutes.  Please try again later.");
        return;
    }
    else if (e.Status == "temp ban") {
        showDialog("Temporary Ban", "Due to failed login attempts, you must refresh your browser to try again.");
        return;
    }
    else if (e.Status == "password mismatch") {
        showDialog("Password Mismatch", "The passwords you entered don't match.  Please retype them.");
        return;
    }
    else if (e.Status == "password length") {
        showDialog("Password Length", "Your new password must be between 8 and 20 characters long.");
        return;
    }
}
function handleGetSupportCategories(e) {
    if (InstaTech.Context == "Technician") {
        $('#selectMoveCaseCategory').html("");
        for (var i = 0; i < e.Categories.length; i++) {
            var category = e.Categories[i];
            var option = document.createElement("option");
            option.value = category;
            option.innerHTML = category;
            $('#selectMoveCaseCategory')[0].options.add(option);
        }
        $('#selectMoveCaseCategory').selectmenu("refresh");
        var request = {
            "Type": "GetSupportTypes",
            "SupportCategory": $('#selectMoveCaseCategory').val()
        };
        InstaTech.Socket_Main.send(JSON.stringify(request));
    }
    else 
    {
        $('#selectSupportCategory').html("");
        for (var i = 0; i < e.Categories.length; i++) {
            var category = e.Categories[i];
            var option = document.createElement("option");
            option.value = category;
            option.innerHTML = category;
            $('#selectSupportCategory')[0].options.add(option);
        }
        $('#selectSupportCategory')[0].selectedIndex = -1;
    }
}
function handleGetSupportTypes(e) {
    if (InstaTech.Context == "Technician") {
        $('#selectMoveCaseType').html("");
        for (var i = 0; i < e.Types.length; i++) {
            var type = e.Types[i];
            var option = document.createElement("option");
            option.value = type;
            option.innerHTML = type;
            $('#selectMoveCaseType')[0].options.add(option);
        }
        $('#selectMoveCaseType').selectmenu("refresh");
    }
    else
    {
        $('#selectSupportType').html("");
        for (var i = 0; i < e.Types.length; i++) {
            var type = e.Types[i];
            var option = document.createElement("option");
            option.value = type;
            option.innerHTML = type;
            $('#selectSupportType')[0].options.add(option);
        }
        $('#selectSupportType')[0].selectedIndex = -1;
    }
}
function handleCustomerChatLogin(e) {
    if (e.Status == "ok") {
        InstaTech.Context = "Customer";
        $("#spanWaitQueue").html(e.Place);
        $("#formCustomerChat").fadeOut(750, function () {
            $("#divChatBoxCustomer").fadeIn(750);
        });
    }
    else {
        showDialog("Submission Error", "<p>There was a problem submitting your information.<br/><br/>Please try closing and re-opening your browser window.  If the issue persists, contact your IT support department.</p>");
    }
}
function handleGetCustomerFormInfo(e) {
    if (e.Name) {
        $("#inputCustomerComputerName").val(e.Name);
        $("#inputCustomerComputerName").attr("readonly", true);
    }
    if (e.UserName) {
        $("#inputCustomerUserID").val(e.UserName);
        $("#inputCustomerUserID").attr("readonly", true);
    }
}
function handleWaitUpdate(e) {
    $("#spanWaitQueue").html(e.Place);
}
function handleGetQueues(e) {
    for (var i = 0; i < e.Queues.length; i++) {
        if ($("#divQueue" + e.Queues[i]).length > 0) {
            continue;
        }
        var queueBlock = document.createElement("div");
        queueBlock.classList.add("queue-block");
        queueBlock.innerHTML = e.Queues[i] + ' (<span class="queue-volume"></span>)';
        queueBlock.id = "divQueue" + e.Queues[i];
        queueBlock.setAttribute("queue", e.Queues[i]);
        $(".col1 .queue-list").append(queueBlock);
    }
    $(".queue-block").off("click").on("click", queueBlockClicked);
    var request = {
        "Type": "GetCases",
        "AuthenticationToken": InstaTech.AuthenticationToken
    };
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
function handleGetCases(e) {
    if (e.Cases.length == 0) {
        return;
    }
    InstaTech.Cases = e.Cases;
}
function handleTakeCase(e) {
    if (InstaTech.Context == "Technician") {
        if (e.Status == "ok") {
            $("#divChatBoxTech").slideDown(function () {
                window.scroll(0, $("#divChatBoxTech").offset().top);
                InstaTech.PartnerFirstName = $("#inputTechQueueFirstName").val();
                InstaTech.PartnerLastName = $("#inputTechQueueLastName").val();
                var partnerID = $("#inputTechQueueUserID").val();
                if (e.PreviousMessages) {
                    for (var i = 0; i < e.PreviousMessages.length; i++) {
                        var divMessage = document.createElement("div");
                        if (e.PreviousMessages[i].FromUserID == partnerID) {
                            divMessage.classList.add("received-chat");
                            divMessage.innerHTML = '<div class="arrow-left"></div><div class="chat-message-header">' + InstaTech.PartnerFirstName + " " + InstaTech.PartnerLastName + ' at ' + parseNETDate(e.PreviousMessages[i].DTSent).toLocaleTimeString() + "</div>" + e.PreviousMessages[i].Content;
                        }
                        else {
                            divMessage.classList.add("sent-chat");
                            divMessage.innerHTML = '<div class="arrow-right"></div><div class="chat-message-header">' + e.PreviousMessages[i].FromUserID + ' at ' + parseNETDate(e.PreviousMessages[i].DTSent).toLocaleTimeString() + "</div>" + e.PreviousMessages[i].Content;
                        }
                        $("#divTechMessages").append(divMessage);
                    }
                }
                $("#divTechMessages").append("<div style='clear:both'>You are connected with " + InstaTech.PartnerFirstName + " " + InstaTech.PartnerLastName + ".");
                $("#divTechMessages")[0].scrollTop = $("#divTechMessages")[0].scrollHeight;
            });
        }
        else if (e.Status == "taken") {
            showDialog("Case Taken", "The case has already been taken.");
        }
        else if (e.status == "locked") {
            showDialog("Case Locked", "The case is temporarily locked by another tech.");
        }
    }
    else if (InstaTech.Context == "Customer") {
        $("#textCustomerInput").removeAttr("disabled");
        InstaTech.PartnerFirstName = e.TechFirstName;
        InstaTech.PartnerLastName = e.TechLastName;
        $("#divWaitMessage").hide();
        $("#divCustomerMessages").append("<div style='clear:both'>" + e.TechFirstName + " " + e.TechLastName + " has connected to the chat session.</div>");
        $("#divCustomreMessages")[0].scrollTop = $("#divCustomreMessages")[0].scrollHeight;
    }
}
function handleChatMessage(e) {
    var divMessage = document.createElement("div");
    divMessage.classList.add("received-chat");
    divMessage.innerHTML = '<div class="arrow-left"></div><div class="chat-message-header">' + InstaTech.PartnerFirstName + " " + InstaTech.PartnerLastName + " at " + new Date().toLocaleTimeString() + "</div>" + atob(e.Message);
    if (InstaTech.Context == "Technician") {
        $("#divTechMessages").append(divMessage);
        $("#divTechMessages")[0].scrollTop = $("#divTechMessages")[0].scrollHeight;
    }
    else if (InstaTech.Context == "Customer") {
        $("#divCustomerMessages").append(divMessage);
        $("#divCustomerMessages")[0].scrollTop = $("#divCustomerMessages")[0].scrollHeight;
    }
}
function handleTyping(e) {
    if (InstaTech.Context == "Technician") {
        $("#divTechStatus").html("Customer is typing...");
        InstaTech.LastTypingStatus = new Date();
        window.setTimeout(function () {
            if (new Date() - InstaTech.LastTypingStatus > 900) {
                $("#divTechStatus").html(" ");
            }
        }, 1000);
    }
    else if (InstaTech.Context == "Customer") {
        $("#divCustomerStatus").html("Agent is typing...");
        InstaTech.LastTypingStatus = new Date();
        window.setTimeout(function () {
            if (new Date() - InstaTech.LastTypingStatus > 900) {
                $("#divCustomerStatus").html(" ");
            }
        }, 1000);
    }
}
function handleCaseUpdate(e) {
    if (InstaTech.Context == "Technician") {
        switch (e.Status) {
            case "Add":
                InstaTech.Cases.push(e.Case);
                if ($(".queue-block.selected").attr("queue") == "All" || $(".queue-block.selected").attr("queue") == e.Case.SupportQueue) {
                    addCaseBlock(e.Case);
                }
                break;
            case "Remove":
                InstaTech.Cases = InstaTech.Cases.filter(function (value, index) {
                    return value.CaseID != e.Case.CaseID;
                });
                if ($("#divCase" + e.Case.CaseID).length > 0) {
                    $("#divCase" + e.Case.CaseID).remove();
                }
                if ($("#inputTechQueueCaseID").val() == e.Case.CaseID && InstaTech.UserID != e.Case.TechUserID) {
                    $(".col3").animate({ "width": 0 });
                    $(".col3 table input, .col3 table textarea").val("");
                }
                break;
            case "Modify":
                InstaTech.Cases.some(function (value, index) {
                    if (value.CaseID == e.Case.CaseID) {
                        InstaTech.Cases[index] == e.Case;
                        return true;
                    }
                });
                break;
            default:
        }
    }
}
function handleLostPartner(e) {
    if (InstaTech.Context == "Customer") {
        $("#divCustomerMessages").append("<div style='clear:both'>Your partner's connection has been lost, and you've been re-added to the queue.</div>");
        $("#textCustomerInput").attr("disabled", true);
        $("#divWaitMessage").show();
        InstaTech.PartnerFirstName = null;
        InstaTech.PartnerLastName = null;
        $("#divCustomerMessages")[0].scrollTop = $("#divCustomerMessages")[0].scrollHeight;
    }
    if (InstaTech.Context == "Technician") {
        $("#divTechMessages").append("<div style='clear:both'>Your partner's connection has been lost.</div>");
        InstaTech.PartnerFirstName = null;
        InstaTech.PartnerLastName = null;
        $("#divTechMessages")[0].scrollTop = $("#divTechMessages")[0].scrollHeight;
    }
}
function handleSendToQueue(e) {
    $("#divCustomerMessages").append("<div style='clear:both'>You've been re-added to the queue.</div>");
    $("#textCustomerInput").attr("disabled", true);
    $("#divWaitMessage").show();
    InstaTech.PartnerFirstName = null;
    InstaTech.PartnerLastName = null;
    $("#divCustomerMessages")[0].scrollTop = $("#divCustomerMessages")[0].scrollHeight;
}
function handleLockCase(e) {
    if (e.Status == "ok") {
        $("#divCase" + e.CaseID).addClass("case-locked");
    }
    else if (e.Status == "taken") {
        showDialog("Case Taken", "The case has already been taken.");
    }
    else if (e.Status == "already locked") {
        showDialog("Case Locked", "The case is already locked.");
    }
}
function handleUnlockCase(e) {
    $("#divCase" + e.CaseID).removeClass("case-locked");
}
