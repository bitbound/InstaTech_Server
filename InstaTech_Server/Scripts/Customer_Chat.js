function initCustomerChat() {
    var request = {
        "Type": "GetSupportCategories"
    };
    InstaTech.Socket_Chat.send(JSON.stringify(request));
    $("a.tooltip-anchor").button({
        icon: "ui-icon-help",
        showLabel: false
    });
    $("a.tooltip-anchor").tooltip();
};
function submitCustomerChat(e) {
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
    }
    else {
        $("#selectSupportType").parent().parent().show();
        var request = {
            "Type": "GetSupportTypes",
            "SupportCategory": category
        };
        InstaTech.Socket_Chat.send(JSON.stringify(request));
    }
}
function handleGetSupportCategories(e) {
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
function handleGetSupportTypes(e) {
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
function handleCustomerLogin(e) {
    if (e.Status == "ok") {
        $("#spanWaitQueue").html(e.Place);
        $("#formCustomerChat").fadeOut(750, function () {
            $("#divChatBoxCustomer").fadeIn(750);
        });
    }
    else {
        showDialog("Submission Error", "<p>There was a problem submitting your information.<br/><br/>Please try closing and re-opening your browser window.  If the issue persists, contact your IT support department.</p>");
    }
}
function handleWaitUpdate(e) {
    $("#spanWaitQueue").html(e.Place);
}
