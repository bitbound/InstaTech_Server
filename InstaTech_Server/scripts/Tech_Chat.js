$(document).ready(function () {
});
function submitTechLogin(e) {
    e.preventDefault();
    var formData = {};
    $("#formTechLogin").find("input").each(function (index, item) {
        formData[item.name] = item.value;
    });
    InstaTech.Socket_Chat.send(JSON.stringify(formData));
}
function forgotPassword(e) {
    if ($("#inputTechUserID").val().length == 0) {
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
                    // TODO.
                }
            },
            {
                text: "No",
                click: function () {
                    $(this).dialog("close");
                }
            }
        ]
    });
}
function queueBlockClicked(e) {
    if ($(this).hasClass("selected")) {
        return;
    }
    $(".queue-block").removeClass("selected");
    $(".queue-block .arrow-right").remove();
    $(e.currentTarget).addClass("selected");
    var arrowRight = document.createElement("div");
    arrowRight.classList.add("arrow-right");
    $(e.currentTarget).append(arrowRight);
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
        for (var i = 0; i < filteredCases.length; i++) {
            var fc = filteredCases[i];
            var caseBlock = document.createElement("div");
            caseBlock.id = "divCase" + fc.CaseID;
            caseBlock.classList.add("case-block");
            caseBlock.innerHTML = fc.CustomerFirstName + " " + fc.CustomerLastName + "<br/><hr/>" + '<span class="case-block-category">' + fc.SupportCategory + '</span><br/><hr/><span class="case-block-type">' + fc.SupportType + '</span><br/><hr/><span class="case-block-wait">Wait Time: <span class="wait-time">' + (new Date() - new Date(parseInt(fc.DTCreated.replace("/Date(", "").replace(")/", ""))) ) / 1000 +'s</span></span>';
            caseBlock.setAttribute("case-id", fc.CaseID);
            $(".col2 .queue-list").append(caseBlock);
        };
        $(".col2 .queue-list .case-block").click(caseBlockClicked);
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
        $(".col3").animate({ "width": "450px" });
    });
}
function handleTechLogin(e) {
    if (e.Status == "new required") {
        $("#inputTechConfirmNewPassword, #inputTechNewPassword").attr("required", true);
        $("#inputTechConfirmNewPassword, #inputTechNewPassword").parent("td").parent("tr").show();
        return;
    }
    else if (e.Status == "ok") {
        sessionStorage["AuthToken"] = e.AuthenticationToken;
        $("#divTechLoginFrame").fadeOut(750, function () {
            $("#divQueueFrame").fadeIn(750, function () {
                $("#divTechChat .portal-content-frame").animate({
                    "width": "90vw"
                }, function () {
                    window.scroll(0, document.getElementById("divQueueFrame").offsetTop);
                });
                $(".queue-block").off("click").on("click", queueBlockClicked);
                var request = {
                    "Type": "GetQueues",
                    "AuthToken": sessionStorage["AuthToken"]
                };
                InstaTech.Socket_Chat.send(JSON.stringify(request));
            });
        });
    }
    else if (e.Status == "invalid") {
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
function handleGetQueues(e) {
    for (var i = 0; i < e.Queues.length; i++) {
        if ($("#divQueue" + e.Queues[i]).length > 0)
        {
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
        "AuthToken": sessionStorage["AuthToken"]
    };
    InstaTech.Socket_Chat.send(JSON.stringify(request));
}
function handleGetCases(e) {
    if (e.Cases.length == 0)
    {
        return;
    }
    InstaTech.Cases = e.Cases;
    $(".queue-block").each(function (index, elem) {
        var numCases = InstaTech.Cases.filter(function (value, index) {
            return value.SupportQueue == $(elem).attr("queue");
        }).length;
        $(elem).find(".queue-volume").html(numCases);
    });
    $("#divQueueAll .queue-volume").html(InstaTech.Cases.length);
}
function handleKicked(e) {
    showDialog("Session Ended", "Your session has been terminated by the server for the following reason: " + e.Reason + "<br/><br/>If you believe this was in error, please contact your system administrator.");
}