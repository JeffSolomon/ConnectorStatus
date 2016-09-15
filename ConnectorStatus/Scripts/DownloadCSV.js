function FormatData(field) {
    return '"' + (field === null ? '' : field) + '"';
}

var download = function (content, fileName, mimeType) {
    var a = document.createElement('a');
    mimeType = mimeType || 'application/octet-stream';

    if (navigator.msSaveBlob) { // IE10
        return navigator.msSaveBlob(new Blob([content], { type: mimeType }), fileName);
    } else if ('download' in a) { //html5 A[download]
        a.href = 'data:' + mimeType + ',' + encodeURIComponent(content);
        a.setAttribute('download', fileName);
        document.body.appendChild(a);
        setTimeout(function () {
            a.click();
            document.body.removeChild(a);
        }, 66);
        return true;
    } else { //do iframe dataURL download (old ch+FF):
        var f = document.createElement('iframe');
        document.body.appendChild(f);
        f.src = 'data:' + mimeType + ',' + encodeURIComponent(content);

        setTimeout(function () {
            document.body.removeChild(f);
        }, 333);
        return true;
    }
}

function BuildCSV(data) {

    var csvArray = [];

    var rowArray = [];

    rowArray.push("EpicLink");
    rowArray.push("Client");
    rowArray.push("Source");
    rowArray.push("Type");
    rowArray.push("ImplementationRound");
    rowArray.push("Key");
    rowArray.push("Description");
    rowArray.push("Stage");
    rowArray.push("Status");
    rowArray.push("LastUpdated")
    rowArray.push("FirstWorkLogDate");
    rowArray.push("MostRecentWorkLogDate");
    rowArray.push("TotalHoursLogged");;
    rowArray.push("Duration");
    csvArray.push(rowArray.join(','));

    for (var i = 0, len = data.length; i < len; i++) {
        rowArray = [];
        var thisRow = data[i];
        var parent = thisRow.ParentTicket;
        rowArray.push(FormatData(parent.Key));
        rowArray.push(FormatData(parent.Client));
        rowArray.push(FormatData(parent.Source));
        rowArray.push(FormatData(parent.DataSourceType));
        rowArray.push(FormatData(parent.ImplementationRound));
        rowArray.push(FormatData(parent.Key));
        rowArray.push(FormatData(parent.Summary));
        rowArray.push("Epic");
        rowArray.push(FormatData(parent.Status));
        rowArray.push(FormatData(parent.TrueLastUpdate))
        rowArray.push(FormatData(parent.FirstLogDate));
        rowArray.push(FormatData(parent.MostRecentLogDate));
        rowArray.push(FormatData(parent.TotalHours));
        rowArray.push(FormatData(parent.LogDuration));
        csvArray.push(rowArray.join(','));

        for (var j = 0, len2 = parent.Stories.length; j < len2; j++) {
            rowArray = [];
            var story = parent.Stories[j];
            rowArray.push(FormatData(story.EpicLink));
            rowArray.push(FormatData(story.Client));
            rowArray.push(FormatData(story.Source));
            rowArray.push(FormatData(parent.DataSourceType));
            rowArray.push(FormatData(story.ImplementationRound));
            rowArray.push(FormatData(story.Key));
            rowArray.push(FormatData(story.Summary));
            rowArray.push(FormatData(story.TicketStage));
            rowArray.push(FormatData(story.Status));
            rowArray.push(FormatData(story.TrueLastUpdate))
            rowArray.push(FormatData(story.FirstLogDate));
            rowArray.push(FormatData(story.MostRecentLogDate));
            rowArray.push(FormatData(story.TotalHours));
            rowArray.push(FormatData(story.LogDuration));
            csvArray.push(rowArray.join(','));
        }
    }

    return csvArray.join('\n');

}