$(document).ready(function () {
    if (!init()) {
        $(".portal-button-frame").remove();
        return;
    }
    $(document).ajaxStart(function () {
        $("#divLoading").show();
    });
    $(document).ajaxStop(function () {
        $("#divLoading").hide();
    })
    setPortalButtonHandlers();
});

function init() {
    if (typeof WebSocket == "undefined")
    {
        $("#divWebSocketUnavailable").show();
        return false;
    }
    try
    {
        var protocol = location.protocol.replace("http:", "ws:").replace("https:", "wss:") + "//";
        InstaTech.Socket_Chat = new WebSocket(protocol + location.host + location.pathname + "/Services/Chat_Socket.cshtml");
        setChatSocketHandlers();
        return true;
    }
    catch (ex)
    {
        console.log("Error initiating websocket connection: " + ex);
        $("#divWebSocketError").show();
        return false;
    }
}

function switchToTechPortal() {
    $("#divCustomerPortal").slideUp(function () {
        $("#divTechPortal").slideDown();
    });
}

function switchToCustomerPortal() {
    $("#divTechPortal").slideUp(function () {
        $("#divCustomerPortal").slideDown();
    });
}

function setPortalButtonHandlers() {
    $(".portal-option-button").click(function () {
        $(this).addClass("remove-css");
        $(this).css({
            "transform": "scale(.75, .75)",
            "transition-duration": ".4s",
            "transition-timing-function": "ease",
            "z-index": 2,
        });
        window.setTimeout(function () {
            $(".remove-css").attr("style", "");
            $(".remove-css").removeClass("remove-css");
        }, 500);
        var strOpens = $(this).attr("opens");
        if ($(strOpens).length == 0) {
            var strFile = $(this).attr("opens-file");
            $.get(location.href + "/Controls/" + strFile, function (data) {
                $("#divTechContent:visible, #divCustomerContent:visible").append(data);
                slideToggleContent(strOpens);
            });
        }
        else
        {
            slideToggleContent(strOpens);
        }
    })
}
function slideToggleContent(strElementName) {
    var strOpens = strElementName;
    if (strOpens.search("#") != 0)
    {
        strOpens = "#" + strOpens;
    }
    var opens = $(strOpens);
    if (opens.is(":visible")) {
        opens.slideUp();
    }
    else {
        opens.slideDown(function () {
            window.scroll(0, document.body.scrollHeight);
            opens.find("input[type=text]").first().select();
        });
    };
}

function setChatSocketHandlers() {
    InstaTech.Socket_Chat.onopen = function () {

    }
    InstaTech.Socket_Chat.onmessage = function (e) {
        var jsonData = JSON.parse(e.data);
        eval("handle" + jsonData.Type + "(jsonData)");
    }
    InstaTech.Socket_Chat.onclose = function () {
        $("#divCustomerContent").hide();
        $("#divTechContent").hide();
        $(".portal-button-frame").hide();
        $("#divWebSocketClosed").show();
    }
    InstaTech.Socket_Chat.onerror = function (ex) {
        console.log("WebSocket Error: " + ex);
        $("#divCustomerContent").hide();
        $("#divTechContent").hide();
        $(".portal-button-frame").hide();
        $("#divWebSocketError").show();
    }
}