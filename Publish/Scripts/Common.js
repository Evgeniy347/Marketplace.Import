
if (!Date.prototype.addDays) {
    Date.prototype.addDays = function (days) {
        var date = new Date(this.valueOf());
        date.setDate(date.getDate() + days);
        return date;
    }
}

if (!Date.prototype.addDays) {
    Date.prototype.addDays = function (days) {
        var date = new Date(this.valueOf());
        date.setDate(date.getDate() + days);
        return date;
    }
}

if (!Date.prototype.toStringMPS) {
    Date.prototype.toStringMPS = function (whithTime) {
        // "2022-07-01"
        var d = this;
        var datestring = d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2);
        if (whithTime)
            datestring += " " + ("0" + d.getHours()).slice(-2) + ":" + ("0" + d.getMinutes()).slice(-2);
        return datestring;
    }
}

function MPS_SaveContext() {
    if (window.MPS_Context) {
        var value = "MPS_Context = " + JSON.stringify(window.MPS_Context);
        console.log(value);
    }
}

function MPS_CreateRestBuilder() {
    var request = new XMLHttpRequest();
    request.MPS_RequestHeaders = [];

    request.AddRequestHeader = function (key, value) {
        request.MPS_RequestHeaders.push({ Key: key, Value: value });
    }

    request.SuccessStatus = [200];
     
    request.SendPost = function (url, json, callback, arg) {
        this.open("POST", url, true);
        this.responseType = "json";
        this.setRequestHeader("Content-type", "application/json;charset=UTF-8");

        for (var i = 0; i < this.MPS_RequestHeaders.length; i++)
            this.setRequestHeader(this.MPS_RequestHeaders[i].Key, this.MPS_RequestHeaders[i].Value);

        this.mps_callback = callback;
        this.mps_callback_arg = arg;
        this.addEventListener("readystatechange", () => {

            if (this.readyState === 4 && this.SuccessStatus.indexOf(this.status) != -1) {
                if (this.mps_callback)
                    this.mps_callback(request.response, request.mps_callback_arg);
            }
        });
        this.send(JSON.stringify(json));
    }
    return request;
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
    }
    else {
        return elements;
    }
}
