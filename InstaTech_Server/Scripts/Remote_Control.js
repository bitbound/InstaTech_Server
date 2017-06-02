var context = {};
var socket = {};
var img;
var byteArray;
var imageData;
var imgWidth;
var imgHeight;
var imgX;
var imgY;
var url;
var fr = new FileReader();
var currentTouches = 0;
var cancelNextTouch;
var lastMultiTouch;
var lastTouch;
var multiTouched;
var doubleTapped;
var touchDragging;
var lastTouchPointX;
var lastTouchPointY;
var lastPointerMove;
var modKeyDown;
var followingCursor;
var lastCursorOffsetX;
var lastCursorOffsetY;
var isTouchScreen = false;
var disconnectRequested = false;
var args = {};
var searchCallback;

function submitTechMainLogin(e) {
    if (e) {
        e.preventDefault();
    }
    var formData = {};
    $("#formTechMainLogin").find("input").each(function (index, item) {
        if (item.type == "checkbox") {
            formData[item.name] = item.checked;
        }
        else {
            formData[item.name] = item.value;
        }
    });
    if (InstaTech.AuthenticationToken) {
        formData["AuthenticationToken"] = InstaTech.AuthenticationToken;
    }
    socket.send(JSON.stringify(formData));
}
function logOutTech(e) {
    clearCachedCreds();
    socket.close();
    $("#spanMainLoginStatus").html("<small>Not logged in.</small>");
    $("#inputTechMainPassword").val("");
    $("#inputTechMainUserID").val("");
    $("#aMainTechLogOut").hide();
    $("#aMainTechLogIn").show();
}
function clearCachedCreds() {
    InstaTech.UserID = null;
    InstaTech.AuthenticationToken = null;
    localStorage.removeItem("RememberMe");
    localStorage.removeItem("UserID");
    localStorage.removeItem("AuthenticationToken");
}
function setMainLoginFrame() {
    $("#spanMainLoginStatus").html("<small>Logged in as: " + InstaTech.UserID + "</small>");
    $("#aMainTechLogIn").hide();
    $("#aMainTechLogOut").show();
}
function forgotPasswordMain(e) {
    if ($("#inputTechMainUserID").val().length == 0) {
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
                        "UserID": $("#inputTechMainUserID").val()
                    };
                    socket.send(JSON.stringify(request));
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
function switchToCustomerPortal() {
    $("#divTechPortal").fadeOut(function () {
        $("#divCustomerPortal").fadeIn();
    });
}
function connectToClient() {
    disconnectRequested = false;
    if (socket.readyState == WebSocket.CLOSED || socket.readyState == WebSocket.CLOSING)
    {
        $("#divStatus").text("Reconnecting...");
        initWebSocket();
        window.setTimeout(connectToClient, 1000);
        return;
    }
    context.canvas.width = 1;
    context.canvas.height = 1;
    $("#divStatus").text("Connecting...");

    var request = {
        "Type": "ConnectionType",
        "ConnectionType": "ViewerApp"
    };
    socket.send(JSON.stringify(request));
    if ($(".toggle-box.selected").attr("id") == "divInteractive") {
        $("#divUninstallService").hide();
        $("#divSendCtrlAltDel").hide();
        var sessionID = $("#inputSessionID").val();
        request = {
            "Type": "Connect",
            "SessionID": sessionID
        };
        socket.send(JSON.stringify(request));
    }
    else {
        $("#divUninstallService").show();
        $("#divSendCtrlAltDel").show();
        var computerName = $("#inputSessionID").val();
        request = {
            "Type": "ConnectUnattended",
            "ComputerName": computerName,
            "AuthenticationToken": InstaTech.AuthenticationToken
        };
        socket.send(JSON.stringify(request));
    }
}

function initWebSocket() {
    socket = new WebSocket(location.origin.replace("http", "ws") + "/Services/Remote_Control_Socket.cshtml");
    socket.binaryType = "arraybuffer";
    socket.onopen = function (e) {
        if (window.location.search.search("AuthenticationToken") > -1) {
            showLoading();
            InstaTech.AuthenticationToken = args.AuthenticationToken;
            var request = {
                "Type": "QuickConnect",
                "UserID": args.UserID,
                "AuthenticationToken": args.AuthenticationToken,
                "ComputerName": args.Computer
            };
            socket.send(JSON.stringify(request));
            $("#divUninstallService").show();
            $("#divSendCtrlAltDel").show();
        }
        else if (localStorage["RememberMe"]) {
            InstaTech.UserID = localStorage["UserID"];
            InstaTech.AuthenticationToken = localStorage["AuthenticationToken"];
            $("#inputTechMainUserID").val(InstaTech.UserID);
            $("#inputTechMainPassword").val("**********");
            document.getElementById("inputTechMainRememberMe").checked = true;
            submitTechMainLogin();
        }
    };
    socket.onclose = function (e) {
        $("#divMain").hide();
        $("#canvasRemoteControl").hide();
        $("#videoRemoteControl").hide();
        $("#divConnect").show();
        $("#divStatus").text("Session closed.");
        // Prevent reconnection of quick connect from Computer Hub.
        if (window.location.search.search("AuthenticationToken") > -1 && disconnectRequested) {
            window.close();
        }
        else
        {
            initWebSocket();
        }
    };
    socket.onerror = function (e) {
        $("#divMain").hide();
        $("#canvasRemoteControl").hide();
        $("#videoRemoteControl").hide();
        $("#divConnect").show();
        $("#divStatus").text("Session closed due to an error.");
        initWebSocket();
    };
    socket.onmessage = function (e) {
        if (e.data instanceof ArrayBuffer) {
            if ($("#canvasRemoteControl").is(":hidden")) {
                $("#canvasRemoteControl").show();
            }
            byteArray = new Uint8Array(e.data);
            var length = byteArray.length;
            // Get the XY coordinate of the top-left of the image based on the last 6 bytes appended to the array.
            imgX = Number((byteArray[length - 6] * 10000) + (byteArray[length - 5] * 100) + byteArray[length - 4]);
            imgY = Number((byteArray[length - 3] * 10000) + (byteArray[length - 2] * 100) + byteArray[length - 1]);
            url = window.URL.createObjectURL(new Blob([byteArray.subarray(0, length - 6)]));
            img = document.createElement("img");
            img.onload = function () {
                context.drawImage(img, imgX, imgY, img.width, img.height);
                window.URL.revokeObjectURL(url);
                socket.send('{"Type":"FrameReceived"}');
            };
            img.src = url;
        }
        else {
            var jsonMessage = JSON.parse(e.data);
            switch (jsonMessage.Type) {
                case "TechMainLogin":
                    if (jsonMessage.Status == "new required") {
                        $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").attr("required", true);
                        $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").parent("td").parent("tr").show();
                        return;
                    }
                    else if (jsonMessage.Status == "ok") {
                        clearCachedCreds();
                        InstaTech.Context = "Technician";
                        InstaTech.UserID = $("#inputTechMainUserID").val();
                        InstaTech.AuthenticationToken = jsonMessage.AuthenticationToken;
                        if (document.getElementById("inputTechMainRememberMe").checked) {
                            localStorage["RememberMe"] = true;
                            localStorage["UserID"] = InstaTech.UserID;
                            localStorage["AuthenticationToken"] = InstaTech.AuthenticationToken;
                        }
                        $("#divMainTechLoginForm").slideUp();
                        if ($("#inputTechMainNewPassword").is(":visible")) {
                            $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").removeAttr("required");
                            $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").parent("td").parent("tr").hide();
                        }
                        setMainLoginFrame();
                        return;
                    }
                    else if (jsonMessage.Status == "invalid") {
                        clearCachedCreds();
                        showDialog("Incorrect Credentials", "The user ID or password is incorrect.  Please try again.");
                        return;
                    }
                    else if (jsonMessage.Status == "expired") {
                        clearCachedCreds();
                        $("#spanMainLoginStatus").html("<small>Not logged in.</small>");
                        $("#inputTechMainPassword").val("");
                        $("#aMainTechLogOut").hide();
                        $("#aMainTechLogIn").show();
                        showDialog("Token Expired", "Your login token has expired, likely due to logging in on another browser.  Please log in again.");
                        return;
                    }
                    else if (jsonMessage.Status == "locked") {
                        showDialog("Account Locked", "Your account as been locked due to failed login attempts.  It will unlock automatically after 10 minutes.  Please try again later.");
                        return;
                    }
                    else if (jsonMessage.Status == "temp ban") {
                        showDialog("Temporary Ban", "Due to failed login attempts, you must refresh your browser to try again.");
                        return;
                    }
                    else if (jsonMessage.Status == "password mismatch") {
                        showDialog("Password Mismatch", "The passwords you entered don't match.  Please retype them.");
                        return;
                    }
                    else if (jsonMessage.Status == "password length") {
                        showDialog("Password Length", "Your new password must be between 8 and 20 characters long.");
                        return;
                    }
                    break;
                case "Unauthorized":
                    $("#divStatus").text("");
                    showDialog("Access Denied", jsonMessage.Reason);
                    break;
                case "ForgotPassword":
                    if (jsonMessage.Status == "invalid") {
                        showDialog("Invalid User ID", "The user ID couldn't be found.");
                    }
                    else if (jsonMessage.Status == "noemail") {
                        showDialog("No Email", "There is no email address on file for this account.  Please contact your system administrator.");
                    }
                    else if (jsonMessage.Status == "error") {
                        showDialog("Error Sending Email", "There was an error sending the email.  Please contact your system administrator.");
                    }
                    else if (jsonMessage.Status == "ok") {
                        showDialog("Password Reset Successful", "A temporary password has been sent to your email.  Please check your inbox.");
                    }
                    break;
                case "Connect":
                    $("#divStatus").text("");
                    if (jsonMessage.Status == "ok") {
                        // Attempt to connect.
                        requestCapture();
                        $("#divConnect").hide();
                        $("#divMain").show();
                    }
                    else if (jsonMessage.Status == "InvalidID") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "Session ID not found.");

                    }
                    else if (jsonMessage.Status == "AlreadyHasPartner") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "That client already has a partner connected.");
                    }
                    break;
                case "ConnectUnattended":
                    $("#divStatus").text("");
                    if (jsonMessage.Status == "UnknownComputer") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "Computer name not found.");
                    }
                    else if (jsonMessage.Status == "AlreadyHasPartner") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "That client already has a partner connected.");
                    }
                    else if (jsonMessage.Status == "unauthorized") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showDialog("Unauthorized", "You are not authorized to access this computer.");
                    }
                    break;
                case "QuickConnect":
                    if (jsonMessage.Status == "denied") {
                        showDialog("Access Denied", "Your authentication token was denied.");
                    }
                    else if (jsonMessage.Status == "unknown") {
                        showDialog("Computer Not Found", "The computer wasn't found.");
                    }
                    else if (jsonMessage.Status == "unauthorized") {
                        showDialog("Unauthorized", "You are not authorized to access this computer.");
                    }
                    break;
                case "ConnectUpgrade":
                    if (jsonMessage.Status == "ok") {
                        var request = {
                            "Type": "CaptureScreen",
                            "Source": "WebSocket",
                        };
                        socket.send(JSON.stringify(request));
                        console.log("Upgrade complete.");
                    }
                    break;
                case "ProcessStartResult":
                    $("#divStatus").text("");
                    if (jsonMessage.Status == "ok")
                    {
                        var computerName = $("#inputSessionID").val();
                        request = {
                            "Type": "CompleteConnection",
                            "ComputerName": computerName,
                            "AuthenticationToken": InstaTech.AuthenticationToken
                        };
                        socket.send(JSON.stringify(request));
                    }
                    else if (jsonMessage.Status == "failed")
                    {
                        showDialog("Connection Failed", "Failed to connect to the remote computer.");
                        $("#divStatus").text("Connection failed.");
                    }
                    break;
                case "CompleteConnection":
                    if (jsonMessage.Status == "ok") {
                        requestCapture();
                        $("#divConnect").hide();
                        $("#divMain").show();
                        if (window.location.search.search("AuthenticationToken") > -1) {
                            removeLoading();
                        }
                    }
                    else if (jsonMessage.Status == "failed") {
                        showDialog("Connection Failed", "Failed to connect to the remote computer.");
                        $("#divStatus").text("Connection failed.");
                    }
                    break;
                case "DesktopSwitch":
                    if (jsonMessage.Status == "pending")
                    {
                        console.log("Desktop switch initated.");
                    }
                    else if (jsonMessage.Status == "ok")
                    {
                        requestCapture();
                    }
                    else if (jsonMessage.Status == "failed") {
                        showDialog("Capture Failed", "The client computer switched desktops (default, logon, or UAC), and screen capture failed.  Please connect again.");
                        $("#divStatus").text("Capture failed.");
                    }
                    break;
                case "ClientUpdating":
                    showDialog("Client Updating", "The client is self-updating.  Please try connecting again in a moment.");
                    socket.close();
                    break;
                case "SearchComputers":
                    searchCallback(jsonMessage.Computers);
                    break;
                case "Bounds":
                    context.canvas.height = jsonMessage.Height;
                    context.canvas.width = jsonMessage.Width;
                    break;
                case "IdleTimeout":
                    showDialog("Connection Timed Out", "Your connection was closed due to inactivity.");
                    break;
                case "UninstallService":
                    if (jsonMessage.Status == "ok")
                    {
                        showTooltip($("#divUninstallService"), "right", "green", "Service uninstalled successfully.");
                    }
                    break;
                case "NewLogin":
                    logOutTech();
                    showDialog("Logged Out", "You have been logged out due to a login from another browser.");
                    break;
                default:
                    break;
            }
            ;
        }
    };
}
function disconnect() {
    disconnectRequested = true;
    toggleMenu();
    socket.close();
}
function requestCapture() {
    var request = {
        "Type": "CaptureScreen"
    };
    socket.send(JSON.stringify(request));
    return;
}
function sendRefreshRequest() {
    var request = {
        "Type": "RefreshScreen"
    };
    context.canvas.width = 300;
    context.canvas.height = 300;
    socket.send(JSON.stringify(request));
}
function toggleMenu() {
    $("#divMenu").css("max-height", (window.innerHeight) - 80 + "px");
    $("#divMenu").slideToggle();
    $("#imgMenu").toggleClass("rotated180");
}
function toggleScaleToFit() {
    if ($("#divScaleToFit").attr("status") == "off") {
        $("#divScaleToFit").attr("status", "on");
        $(".input-surface").css("width", "100%");
        $(".input-surface").css("height", "calc(100vh - 55px)");
    }
    else {
        $("#divScaleToFit").attr("status", "off");
        $(".input-surface").css("width", "auto");
        $(".input-surface").css("height", "auto");
    }
}
function toggleFollowCursor() {
    if ($("#divFollowCursor").attr("status") == "off") {
        $("#divFollowCursor").attr("status", "on");
    }
    else {
        $("#divFollowCursor").attr("status", "off");
    }
}
function sendClipboard(e) {
    // Do nothing if input is empty.
    if ($("#textClipboard").val() == "") {
        return;
    }
    if (socket.readyState != socket.OPEN) {
        return;
    }
    var request = {
        "Type": "SendClipboard",
        "Data": btoa(e.currentTarget.value),
    };
    socket.send(JSON.stringify(request));
    showTooltip(e.currentTarget, "bottom", "black", "Clipboard data sent.");
    $("#textClipboard").val("");
}
function sendCtrlAltDel() {
    var request = {
        "Type": "CtrlAltDel"
    }
    socket.send(JSON.stringify(request));
}
function transferFiles(fileList) {
    if (socket.readyState != socket.OPEN) {
        return;
    }
    for (var i = 0; i < fileList.length; i++) {
        var file = fileList[i];
        var strPath = "/Services/File_Transfer.cshtml";
        var fd = new FormData();
        fd.append('fileUpload', file);
        var xhr = new XMLHttpRequest();
        xhr.open('POST', strPath, true);
        xhr.onload = function () {
            if (xhr.status === 200) {
                var fileName = xhr.responseText;
                var url = location.origin + "/Services/File_Transfer.cshtml?file=" + fileName;
                var request = {
                    "Type": "FileTransfer",
                    "FileName": fileName,
                    "URL": url,
                };
                socket.send(JSON.stringify(request));
                showTooltip($(document.body), "center", "seagreen", "File(s) uploaded successfully.");
            }
            else {
                showDialog("Upload Failed", "File upload failed.");
            }
        };
        xhr.send(fd);
    }
}
function sendUninstall(e) {
    disconnectRequested = true;
    var request = {
        "Type": "UninstallService"
    }
    socket.send(JSON.stringify(request));
    showTooltip($("#divUninstallService"), "right", "green", "Sending uninstall request...");
}
function scrollToCursor() {
    if ($(":hover").length > 0 && $("#divFollowCursor").attr("status") == "on" && isTouchScreen == false)
    {
        followingCursor = true;
        var percentX = (lastCursorOffsetX / window.innerWidth) - .5;
        var percentY = (lastCursorOffsetY / window.innerHeight) - .5;

        // If true, cursor is out of deadzone.
        if (Math.abs(percentX) > .2)
        {
            window.scrollBy((percentX * 50), 0);
        }
        // If true, cursor is out of deadzone.
        if (Math.abs(percentY) > .2) {
            window.scrollBy(0, (percentY * 50));
        }
        window.setTimeout(scrollToCursor, 50);
    }
    else
    {
        followingCursor = false;
    }
}
$(document).ready(function () {
    window.location.search.replace("?", "").split("&").forEach(function (value, index) {
        var split = value.split("=");
        args[split[0]] = split[1];
    });
    context = document.getElementById("canvasRemoteControl").getContext("2d");
    $(".toggle-box").on("click", function (e) {
        if (e.currentTarget.id == "divInteractive") {
            document.getElementById("inputSessionID").setAttribute("placeholder", "Enter client's session ID.");
            document.getElementById("inputSessionID").setAttribute("pattern", "[0-9 ]*");
        }
        else
        {
            if (InstaTech.UserID == undefined || InstaTech.AuthenticationToken == undefined)
            {
                showDialog("Login Required", "You must be logged in to start an unattended access sessions.");
                return;
            }
            document.getElementById("inputSessionID").setAttribute("placeholder", "Enter client's PC name.");
            document.getElementById("inputSessionID").removeAttribute("pattern");
        }
        $(".toggle-box").removeClass("selected");
        e.currentTarget.classList.add("selected");
    });
    $(".input-surface").on("mousemove", function (e) {
        e.preventDefault();
        if (new Date() - lastPointerMove < 50)
        {
            return;
        }
        lastPointerMove = new Date();
        if (socket.readyState == WebSocket.OPEN) {
            var pointX = e.offsetX / $(e.currentTarget).width();
            var pointY = e.offsetY / $(e.currentTarget).height();
            var request = {
                "Type": "MouseMove",
                "PointX": pointX,
                "PointY": pointY
            };
            socket.send(JSON.stringify(request));
        }
    });
    $(".input-surface").on("mousedown", function (e) {
        if (socket.readyState == WebSocket.OPEN) {
            if (e.button != 0 && e.button != 2) {
                return;
            }
            var pointX = e.offsetX / $(e.currentTarget).width();
            var pointY = e.offsetY / $(e.currentTarget).height();
            var request = {
                "Type": "MouseDown",
                "PointX": pointX,
                "PointY": pointY,
                "Alt": e.altKey,
                "Ctrl": e.ctrlKey,
                "Shift": e.shiftKey
            };
            if (e.button == 0) {
                request.Button = "Left";
            }
            else if (e.button == 2) {
                request.Button = "Right";
            }
            socket.send(JSON.stringify(request));
        }
    });
    $(".input-surface").on("mouseup", function (e) {
        if (socket.readyState == WebSocket.OPEN) {
            if (e.button != 0 && e.button != 2) {
                return;
            }
            var pointX = e.offsetX / $(e.currentTarget).width();
            var pointY = e.offsetY / $(e.currentTarget).height();
            var request = {
                "Type": "MouseUp",
                "PointX": pointX,
                "PointY": pointY,
                "Alt": e.altKey,
                "Ctrl": e.ctrlKey,
                "Shift": e.shiftKey
            };
            if (e.button == 0) {
                request.Button = "Left";
            }
            else if (e.button == 2) {
                e.preventDefault();
                request.Button = "Right";
            }
            socket.send(JSON.stringify(request));
        }
    });
    $(".input-surface").on("touchstart", function (e) {
        if (socket.readyState != WebSocket.OPEN) {
            return;
        }
        isTouchScreen = true;
        currentTouches++;
        var touchPointOffset = e.currentTarget.getBoundingClientRect();
        lastTouchPointX = (e.touches[0].clientX - touchPointOffset.left) / $(e.currentTarget).width();
        lastTouchPointY = (e.touches[0].clientY - touchPointOffset.top) / $(e.currentTarget).height();
        if (e.touches.length > 1) {
            multiTouched = true;
            lastMultiTouch = Date.now();
        }
        if (e.touches.length == 1) {
            e.preventDefault();
            if (Date.now() - lastTouch < 500) {
                doubleTapped = true;
                window.setTimeout(function () {
                    if (currentTouches == 1 && doubleTapped && !multiTouched) {
                        var request = {
                            "Type": "TouchDown",
                        };
                        socket.send(JSON.stringify(request));
                        touchDragging = true;
                    }
                }, 500);
            }
            else {
                window.setTimeout(function () {
                    if (currentTouches == 1 && !doubleTapped && !multiTouched && !cancelNextTouch && !touchDragging) {
                        var request = {
                            "Type": "LongPress",
                            "Button": "Right"
                        };
                        socket.send(JSON.stringify(request));
                        cancelNextTouch = true;
                    }
                }, 500);
            }
        }
        ;
        lastTouch = Date.now();
    });
    $(".input-surface").on("touchmove", function (e) {
        if (socket.readyState != WebSocket.OPEN) {
            return;
        }
        cancelNextTouch = true;
        if (multiTouched && !touchDragging) {
            return;
        }
        if (e.touches.length == 1) {
            e.preventDefault();
            if (new Date() - lastPointerMove < 50) {
                return;
            }
            lastPointerMove = new Date();
            var touchPointOffset = e.currentTarget.getBoundingClientRect();
            var pointX = (e.touches[0].clientX - touchPointOffset.left) / $(e.currentTarget).width();
            var pointY = (e.touches[0].clientY - touchPointOffset.top) / $(e.currentTarget).height();
            var request = {
                "Type": "TouchMove",
                "MoveByX": pointX - lastTouchPointX,
                "MoveByY": pointY - lastTouchPointY,
            };
            socket.send(JSON.stringify(request));
            lastTouchPointX = pointX;
            lastTouchPointY = pointY;
        }
    });
    $(".input-surface").on("touchend", function (e) {
        if (socket.readyState == WebSocket.OPEN) {
            e.preventDefault();
        }
        else {
            return;
        }
        currentTouches--;
        var request;
        var touchPointOffset = e.currentTarget.getBoundingClientRect();
        var pointX = (e.changedTouches[0].clientX - touchPointOffset.left) / $(e.currentTarget).width();
        var pointY = (e.changedTouches[0].clientY - touchPointOffset.top) / $(e.currentTarget).height();
        if (e.touches.length == 0) {
            e.preventDefault();
            if (cancelNextTouch || multiTouched || touchDragging) {
                if (touchDragging) {
                    touchDragging = false;
                    request = {
                        "Type": "TouchUp",
                    };
                    socket.send(JSON.stringify(request));
                }
            }
            else {
                request = {
                    "Type": "Tap",
                };
                socket.send(JSON.stringify(request));
            }
            cancelNextTouch = false;
            multiTouched = false;
            doubleTapped = false;
        }
    });
    $(".input-surface").on("wheel", function (e) {
        e.preventDefault();
        var request = {
            "Type": "MouseWheel",
            "DeltaY": e.originalEvent.deltaY,
            "DeltaX": e.originalEvent.deltaX
        };
        socket.send(JSON.stringify(request));
    })
    $(".input-surface").on("click", function (e) {
        e.preventDefault();
        $(":focus").blur();
    });
    $(window).on("keydown", function (e) {
        if ($(":focus").length == 0 && socket.readyState == WebSocket.OPEN) {
            e.preventDefault();
            if (e.key == "Alt" || e.key == "Shift" || e.key == "Ctrl" || e.key == "Control") {
                modKeyDown = true;
                return;
            }
            modKeyDown = false;
            var key = e.key;
            var modifiers = [];
            // Need special handling for these characters on Windows.
            if (key.search("[%^+{}]") > -1)
            {
                key = "{" + key + "}";
            }
            else
            {
                if (e.altKey) {
                    modifiers.push("Alt");
                }
                if (e.ctrlKey) {
                    modifiers.push("Control");
                }
                if (e.shiftKey) {
                    if (key != "%" && key != "^" && key != "+") {
                        modifiers.push("Shift")
                    }
                }
            }
            var request = {
                "Type": "KeyPress",
                "Key": key,
                "Modifiers": modifiers
            };
            socket.send(JSON.stringify(request));
        }
    });
    $(window).on("keyup", function (e) {
        if ($(":focus").length == 0 && socket.readyState == WebSocket.OPEN) {
            e.preventDefault();
            if (e.key == "Alt" || e.key == "Shift" || e.key == "Ctrl" || e.key == "Control") {
                if (modKeyDown == true) {
                    var key = e.key;
                    if (key == "Alt") {
                        key = "%";
                    }
                    else if (key == "Ctrl" || key == "Control") {
                        key = "^";
                    }
                    else if (key == "Shift") {
                        key = "+";
                    }
                    var request = {
                        "Type": "KeyPress",
                        "Key": key,
                        "Modifiers": []
                    };
                    socket.send(JSON.stringify(request));
                }
            }
        }
    });
    $(window).on("click", function (e) {
        if (e.button == 2 && $(":focus").length == 0) {
            e.preventDefault();
        }
        if ($("#divMenu").is(":visible") && !$("#divMenuButtonWrapper").is(":hover") && !$("#divMenu").is(":hover")) {
            toggleMenu();
        }
    });
    $(window).on("touchstart", function (event) {
        if (event.touches.length > 2) {
            if (document.documentElement.requestFullscreen) {
                document.documentElement.requestFullscreen();
            }
            else if (document.documentElement.webkitRequestFullscreen) {
                document.documentElement.webkitRequestFullscreen();
            }
            else if (document.documentElement.mozRequestFullScreen) {
                document.documentElement.mozRequestFullScreen();
            }
            ;
        }
        ;
    });
    $(window).on("mousemove", function (e) {
        lastCursorOffsetX = e.clientX;
        lastCursorOffsetY = e.clientY;
        if (!followingCursor) {
            scrollToCursor();
        }
    })
    $(window).on("resize", function () {
        $("#divMenu").css("max-height", (window.innerHeight) - 80 + "px");
    });
    window.ondragover = function (e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = "copy";
    };
    window.ondrop = function (e) {
        e.preventDefault();
        if (e.dataTransfer.files.length < 1) {
            return;
        }
        transferFiles(e.dataTransfer.files);
    };
    $("#inputSessionID").autocomplete({
        source: function (data, response) {
            
            if ($("#divUnattended").is(".selected"))
            {
                searchCallback = response;
                var request = {
                    "Type": "SearchComputers",
                    "Input": data.term,
                    "AuthenticationToken": InstaTech.AuthenticationToken
                };
                socket.send(JSON.stringify(request)); 
            }
        },
        delay: 300
        
    })
    initWebSocket();
});
