 
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
