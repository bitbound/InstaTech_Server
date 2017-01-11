$(document).ready(function () {
    window.onerror = function (messageOrEvent, source, lineno, colno, error) {
        error.lineno = lineno;
        error.colno = colno;
        error.source = source;
        var strError = JSON.stringify(error);
        sendClientError(strError);
        throw error;
    };
    if (!init()) {
        return;
    }
    window.onbeforeunload = function () {
        if (InstaTech.Socket_Chat.readyState == 1 && location.host.search("localhost") == -1) {
            return "Navigating away from this page will end your current session.  Are you sure you want to leave?";
        }
    };
    $(document).ajaxStart(function () {
        $("#divLoading").show();
    });
    $(document).ajaxStop(function () {
        $("#divLoading").hide();
    });
    setPortalButtonHandlers();
});
function init() {
    if (typeof WebSocket == "undefined") {
        $(".portal-button-frame").remove();
        $("#divWebSocketUnavailable").show();
        return false;
    }
    try {
        var protocol = location.protocol.replace("http:", "ws:").replace("https:", "wss:") + "//";
        InstaTech.Socket_Chat = new WebSocket(protocol + location.host + location.pathname + "/Services/Chat_Socket.cshtml");
        setChatSocketHandlers();
        return true;
    }
    catch (ex) {
        $(".portal-button-frame").remove();
        console.log("Error initiating websocket connection: " + ex);
        $("#divWebSocketError").show();
        return false;
    }
}
function switchToTechPortal() {
    $("#divCustomerPortal").fadeOut(function () {
        $("#divTechPortal").fadeIn();
    });
}
function switchToCustomerPortal() {
    $("#divTechPortal").fadeOut(function () {
        $("#divCustomerPortal").fadeIn();
    });
}
// Sets the onclick event handler for .portal-option-button elements.  The element
// must have attributes "opens" and "opens-file".  "Opens-file" must be the name of the file
// that's in /Controls/ that contains the HTML for the content to be loaded.  "Opens" must
// be the ID of the first element in the content, which will be given a slideDown opening effect.
// If the "opens" element has an onload event, it will be fired.
function setPortalButtonHandlers() {
    $(".portal-option-button").click(function (e) {
        $(e.currentTarget).addClass("remove-css");
        $(e.currentTarget).css({
            "transform": "scale(.75, .75)",
            "transition-duration": ".4s",
            "transition-timing-function": "ease",
            "z-index": 2,
        });
        window.setTimeout(function () {
            $(".remove-css").attr("style", "");
            $(".remove-css").removeClass("remove-css");
        }, 500);
        var strOpens = $(e.currentTarget).attr("opens");
        if (strOpens.search("#") != 0) {
            strOpens = "#" + strOpens;
        }
        if ($(strOpens).length == 0) {
            var strFile = $(e.currentTarget).attr("opens-file");
            $.get(location.href + "/Controls/" + strFile, function (data) {
                $("#divTechContent:visible, #divCustomerContent:visible").append(data);
                slideToggleContent(strOpens);
                if ($(strOpens).length > 0 && $(strOpens)[0].onload) {
                    $(strOpens)[0].onload();
                }
            });
        }
        else {
            slideToggleContent(strOpens);
        }
    });
}
function slideToggleContent(strElementIDSelector) {
    var opens = $(strElementIDSelector);
    if (opens.is(":visible")) {
        opens.slideUp();
    }
    else {
        opens.slideDown(function () {
            window.scroll(0, opens.offset().top);
            opens.find("input[type=text]").first().select();
        });
    }
    ;
}
function setChatSocketHandlers() {
    InstaTech.Socket_Chat.onopen = function () {
    };
    InstaTech.Socket_Chat.onmessage = function (e) {
        var jsonData = JSON.parse(e.data);
        eval("handle" + jsonData.Type + "(jsonData)");
    };
    InstaTech.Socket_Chat.onclose = function () {
        $("#divCustomerContent").hide();
        $("#divTechContent").hide();
        $(".portal-button-frame").hide();
        $("#divWebSocketClosed").show();
    };
    InstaTech.Socket_Chat.onerror = function (ex) {
        console.log("WebSocket Error: " + ex);
        $("#divCustomerContent").hide();
        $("#divTechContent").hide();
        $(".portal-button-frame").hide();
        $("#divWebSocketError").show();
    };
}
