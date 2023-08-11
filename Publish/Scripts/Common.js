
if (!Date.prototype.addDays) {
    Date.prototype.addDays = function (days) {
        var date = new Date(this.valueOf());
        date.setDate(date.getDate() + days);
        return date;
    };
}

if (!Date.prototype.addDays) {
    Date.prototype.addDays = function (days) {
        var date = new Date(this.valueOf());
        date.setDate(date.getDate() + days);
        return date;
    };
}

if (!Date.prototype.toStringMPS) {
    Date.prototype.toStringMPS = function (pattern) {

        var result = pattern;
        var d = this;

        if (result.indexOf("yyyy") != -1) {
            result = result.replaceAll("yyyy", d.getFullYear());
        }

        if (result.indexOf("yyy") != -1) {
            result = result.replaceAll("yyy", d.getFullYear().toString().slice(-3));
        }

        if (result.indexOf("yy") != -1) {
            result = result.replaceAll("yy", d.getFullYear().toString().slice(-2));
        }

        if (result.indexOf("y") != -1) {
            result = result.replaceAll("y", d.getFullYear().toString().slice(-1));
        }

        if (result.indexOf("MM") != -1) {
            result = result.replaceAll("MM", ("0" + (d.getMonth() + 1)).slice(-2));
        }

        if (result.indexOf("M") != -1) {
            result = result.replaceAll("M", (d.getMonth() + 1));
        }

        if (result.indexOf("dd") != -1) {
            result = result.replaceAll("dd", ("0" + d.getDate()).slice(-2));
        }

        if (result.indexOf("d") != -1) {
            result = result.replaceAll("d", d.getDate());
        }

        if (result.indexOf("mm") != -1) {
            result = result.replaceAll("mm", ("0" + d.getMinutes()).slice(-2));
        }

        if (result.indexOf("m") != -1) {
            result = result.replaceAll("m", d.getMinutes());
        }

        if (result.indexOf("HH") != -1) {
            result = result.replaceAll("HH", ("0" + d.getHours()).slice(-2));
        }

        if (result.indexOf("H") != -1) {
            result = result.replaceAll("H", d.getHours());
        }

        var hh = null;
        if (result.indexOf("h") != -1) {
            hh = d.getHours();

            if (hh == 0)
                hh = 12 + "PM";
            else if (hh > 12)
                hh = (hh - 12) + "PM";
            else
                hh = hh + "AM";
        }

        if (result.indexOf("hh") != -1) {
            result = result.replaceAll("hh", ("0" + hh).slice(-4));
        }

        if (result.indexOf("h") != -1) {
            result = result.replaceAll("h", hh);
        }

        if (result.indexOf("ss") != -1) {
            result = result.replaceAll("ss", ("0" + d.getSeconds()).slice(-2));
        }

        if (result.indexOf("s") != -1) {
            result = result.replaceAll("s", d.getSeconds());
        }
        return result;
    };
}

function MPS_SetCookie(name, value, days) {
    var expires = "";
    MPS_DeleteCookie(name);
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}

function MPS_DeleteCookie(name, path, domain) {
    if (MPS_GetCookie(name)) {
        document.cookie =
            name +
            "=" +
            ((path) ? ";path=" + path : "") +
            ((domain) ? ";domain=" + domain : "") +
            ";expires=Thu, 01 Jan 1970 00:00:01 GMT";
    }
}

function MPS_SetEternalCookies() {
    var cookies = MPS_GetCookies();
    var now = new Date();
    now.setTime(now.getTime() + (500 * 24 * 60 * 60 * 1000));

    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i];
        document.cookie = cookie.Name +
            "=" +
            cookie.Value +
            ";path=/" +
            ";expires=" +
            now.toUTCString() +
            ";max-age=" +
            86400 * 500;

    }
}

function MPS_GetCookies() {
    return document.cookie.split(";").map(x => {
        var parts = x.split("=");

        return {
            Name: parts[0].trim(),
            Value: parts[1]
        };
    });
}

function MPS_GetCookie(name) {

    var cookies = MPS_GetCookies();

    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i];

        if (cookie.Name == name)
            return cookie.Value;
    }
}

function MPS_SaveContext() {
    if (window.MPS_Context) {
        var value = "MPS_Context = " + JSON.stringify(window.MPS_Context);
        console.log(value);
    }
}

function MPS_GetCountLog(message) {
    if (window.MPS_Context) {
        if (window.MPS_Context.Logs) {
            var value = window.MPS_Context.Logs[message];
            return value ? value : 0;
        }
    }
    return null;
}

function MPS_PushLog(message) {
    if (window.MPS_Context) {
        if (!window.MPS_Context.Logs)
            window.MPS_Context.Logs = {};

        if (window.MPS_Context.Logs[message])
            window.MPS_Context.Logs[message] += 1;
        else
            window.MPS_Context.Logs[message] = 1;

        MPS_SaveContext();
    }
}

function MPS_UpdateLog(fun) {
    fun(window.MPS_Context);
    MPS_SaveContext();
}

function MPS_CreateGetBuilder() {
    var request = new XMLHttpRequest();
    request.MPS_RequestHeaders = [];

    request.AddRequestHeader = MPS_AddRequestHeader;

    request.SuccessStatus = [200];
    request.MethodName = "GET";
    request.ResponseType = "json";

    request.SendPost = function (url, callback, arg) {
        this.open(request.MethodName, url, true);
        this.responseType = this.ResponseType;

        for (var i = 0; i < this.MPS_RequestHeaders.length; i++)
            this.setRequestHeader(this.MPS_RequestHeaders[i].Key, this.MPS_RequestHeaders[i].Value);

        this.mps_callback = callback;
        this.mps_callback_arg = arg;
        this.addEventListener("readystatechange",
            () => {

                if (this.readyState === 4) {
                     
                    MPS_WriteResponceLog(request, this.status);

                    if (this.SuccessStatus.indexOf(this.status) != -1) {
                        if (this.mps_callback)
                            this.mps_callback(request.response, request.mps_callback_arg);
                    }
                }
            });

        console.log("Send" + request.MethodName + ": " + url);
        this.send();
    };

    request.SendPostPromise = function (url, params) {
        var thisObj = this;
        var promise = new Promise(function (resolve, reject) {
            thisObj.SendPost(url, params, resolve);
        });
        return promise;
    }
    return request;
}

function MPS_WriteResponceLog(request, status) {
    var strResp = typeof request.response === "string"
        ? request.response
        : JSON.stringify(request.response);

    strResp = strResp ? strResp.substring(0, 2000) : "";

    console.log("\r\nSend " + request.MethodName + " Responce State:" + status + "\n\rParams: " + strResp); 
}

function MPS_CreateRestBuilder() {
    var request = new XMLHttpRequest();
    request.MPS_RequestHeaders = [];
    request.MPS_RequestHeaders.push({ Key: "Content-type", Value: "application/json;charset=UTF-8" });

    request.AddRequestHeader = MPS_AddRequestHeader;

    request.SuccessStatus = [200];
    request.MethodName = "POST";
    request.ResponseType = "json";
    request.SendPost = function (url, params, callback, arg) {
        this.open(request.MethodName, url, true);
        this.responseType = this.ResponseType;

        for (var i = 0; i < this.MPS_RequestHeaders.length; i++) {
            this.setRequestHeader(this.MPS_RequestHeaders[i].Key, this.MPS_RequestHeaders[i].Value);
        }

        this.mps_callback = callback;
        this.mps_callback_arg = arg;
        this.addEventListener("readystatechange",
            () => {

                if (this.readyState === 4) {  

                    MPS_WriteResponceLog(request, this.status);

                    if (this.SuccessStatus.indexOf(this.status) != -1) {
                        if (this.mps_callback)
                            this.mps_callback(request.response, request.mps_callback_arg);
                    }
                }
            });

        var json = typeof params === "string" ? params : JSON.stringify(params);

        this.send(json);
        console.log("Send" + request.MethodName + ": " + url + "\n\rParams: " + json);
    };

    request.SendPostPromise = function (url, params) {
        var thisObj = this;
        var promise = new Promise(function (resolve, reject) {
            thisObj.SendPost(url, params, resolve);
        });
        return promise;
    }

    return request;
}

function MPS_AddRequestHeader(key, value) {
    var header = null;
    for (var i = 0; i < this.MPS_RequestHeaders.length; i++) {
        if (this.MPS_RequestHeaders[i].Key.toLowerCase() == key.toLowerCase()) {
            header = this.MPS_RequestHeaders[i];
            break;
        }
    }
    if (header == null) {
        this.MPS_RequestHeaders.push({ Key: key, Value: value });
    } else {
        header.Value = value;
    }
}

function MPS_GetElementFilter(options) {
    return MPS_GetElementsFilter(options)[0];
}

function MPS_GetElementsFilter(options) {
    //var options = {
    //    selector: "div",
    //    container: null,
    //    classNameContaince: null,
    //    filter: function () { return true; },
    //    childOptions: {},
    //};

    var container = (options.container ? options.container : document);
    var elements = Array.from(container.querySelectorAll(options.selector));

    if (options.classNameContaince)
        elements = elements.filter(function (x) { return x.className.indexOf(options.classNameContaince) != -1; });

    if (options.innerTextContaince)
        elements = elements.filter(function (x) { return x.innerText.indexOf(options.innerTextContaince) != -1; });

    if (options.filter)
        elements = elements.filter(options.filter);

    if (options.childOptions) {

        var result = [];
        for (var i = 0; i < elements.length; i++) {

            options.childOptions.container = elements[i];
            var resultChild = MPS_GetElementsFilter(options.childOptions);
            for (var j = 0; j < resultChild.length; j++) {
                result.push(resultChild[j]);
            }
        }

        return result;
    } else {
        return elements;
    }
}

function MPS_DownloadBase64Data(dataBase64, filename, type) {
    MPS_PushLog("DownloadBase64Data " + filename);
    var byteCharacters = atob(dataBase64);
    var byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    var byteArray = new Uint8Array(byteNumbers);
    var file = new Blob([byteArray], { type: type });
    MPS_DownloadBlob(file, filename);
}

function MPS_DownloadData(data, filename, type) {
    MPS_PushLog("DownloadData " + filename);
    var file = new Blob([data], { type: type });
    MPS_DownloadBlob(file, filename);
}

function MPS_DownloadBlob(blob, filename) {
    MPS_PushLog("DownloadBlob " + filename);

    if (window.navigator.msSaveOrOpenBlob) // IE10+
        window.navigator.msSaveOrOpenBlob(blob, filename);
    else { // Others
        var a = document.createElement("a");
        var url = URL.createObjectURL(blob);

        a.href = url;
        MPS_PushLog("DownloadBlob url " + url);
        a.download = filename;
        document.body.appendChild(a);
        a.click();
    }
}

function base64ToByteArray(base64String) {
    try {
        var sliceSize = 1024;
        var byteCharacters = atob(base64String);
        var bytesLength = byteCharacters.length;
        var slicesCount = Math.ceil(bytesLength / sliceSize);
        var byteArrays = new Array(slicesCount);

        for (var sliceIndex = 0; sliceIndex < slicesCount; ++sliceIndex) {
            var begin = sliceIndex * sliceSize;
            var end = Math.min(begin + sliceSize, bytesLength);

            var bytes = new Array(end - begin);
            for (var offset = begin, i = 0; offset < end; ++i, ++offset) {
                bytes[i] = byteCharacters[offset].charCodeAt(0);
            }
            byteArrays[sliceIndex] = new Uint8Array(bytes);
        }
        return byteArrays;
    } catch (e) {
        console.log("Couldn't convert to byte array: " + e);
        return undefined;
    }
}

function MPS_GetParams(key) {

    var result = null;
    if (window.MPS_Params) {
        result = window.MPS_Params[key];
    }

    console.log("GetParams: Key = '" + key + "' Value:'" + result + "'");

    return result;
}

function MPS_TrimChar(string, charToRemove) {
    while (string.charAt(0) == charToRemove) {
        string = string.substring(1);
    }

    while (string.charAt(string.length - 1) == charToRemove) {
        string = string.substring(0, string.length - 1);
    }

    return string;
}