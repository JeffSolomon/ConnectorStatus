var buildData = [];
var stages = [];
var clients = {};
var regions = {};
var qf;


var main = function () {

    $.getJSON('/Content/JSON/Regions.json', null, function (data) { 
        for(var r in data.regions){ 
            regions[data.regions[r].region] = data.regions[r].clients; 
        }
    });
    // regions["northwest"] = ["CCCN", "CHPW", "SLVTN", "GOBHI", "MODA"];
    // regions["newyork"] = ["AHP", "XCLS", "UHG"];
    // regions["northeast"] = ["BIDCO", "EBNHC", "ONP", "STW", "VCA", "WCHN", "AXBRO", "MEMS", "BVCHC"];
    // regions["south"] = ["ACP", "FHN", "LHCQF", "UCF", "CDCR"];

    GetData();

    $('#loadTickets').click(function () {
        GetData($('#loginform').serializeArray()[0].value, $('#loginform').serializeArray()[1].value);
        $('#login').closeModal();
    });

    $('.modal-trigger').leanModal();

    $('#do-filter').click(function () { ApplyFilters(); });
    $('#filter-trigger').click(function () { ConfigureFilterClickActions(); });
    $('#refresh-builds').click(function () { ShowLoading(true);  GetData('reload', 'reload', true); });
    $('#submit-all-comments').click(function () { SubmitComments(); });
    $('#download').click(function () { download(BuildCSV(buildData), 'Connector-Builds.csv', 'text/csv'); });
}


function GetData(un, pw, filter) {
    ShowLoading(true);
    $.ajax({
        type: "POST",
        url: '/ConnectorStatus/GetBuilds',
        data: JSON.stringify({ 'username': un, 'password': pw }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            if (data) {
                buildData = data;
                ShowBuilds(data);
                BuildFilters();
                if (filter) {
                    ApplyFilters();
                }
            }
            else {
                $('#login').openModal({
                    dismissible: false
                });
            }
        },
        error: function () {
            $('#login').openModal({
                dismissible: false
            });
        }
    });
}

function ShowBuilds(data) {
    ShowLoading(true);
    stages = [];
    $('#connectorStatusTable > thead > tr > th > div > span').each(function () { stages.push($(this).text()); })

    var tableArray = [];

    for (var i = 0, len = data.length; i < len; i++) {
        tableArray.push('<tr>');

        var thisRow = data[i];

        tableArray.push(BuildTooltippedTd(thisRow.ParentTicket.Key, thisRow.ParentTicket.ImplementationRound, thisRow.ParentTicket.Client));
        tableArray.push(BuildTooltippedTd(thisRow.ParentTicket.Key, thisRow.ParentTicket.ImplementationRound, thisRow.ParentTicket.Source));
        tableArray.push('<td class="no-paddingz dstype">' + thisRow.ParentTicket.DataSourceType + '</td>');
        tableArray.push('<td class="no-paddingz">' + FormatDate(thisRow.FirstLogDate) + '</td>');

        for (var j = 0, lens = stages.length; j < lens; j++) {
            if (stages[j] !== "Hours") {
                var ticket = thisRow.StageColors[stages[j]];

                if (typeof (ticket) === "undefined") {
                    Console.Log(ticket);
                }
                var status = ticket.Status;
                if (thisRow.ParentTicket.Status === '190-Completed') {
                    status = 'Closed';
                    ticket.Status = status;
                }
                    

                var content = '<div class="status-bubble ' + status.toLowerCase().split(' ').join('-') + '" />';

                tableArray.push(BuildTooltippedTd(ticket.Key, ticket.ToolTipLabel, content));
            }
        }

        tableArray.push('<td class="no-paddingz" style="text-align:center;">' + FormatDecimal(thisRow.TotalHours) + '</td>');

        var comment = '<td class="no-paddingz">' +
                        '<a class="tooltipped" data-position="bottom" data-delay="50" data-tooltip="' + thisRow.ParentTicket.Description + '">' +
                        '<div><input value="' + thisRow.ParentTicket.Description + '" id="' + thisRow.ParentTicket.Key +
                        '" type="text" class="ticket-comment" style="border:none;margin:0px;padding:0px;height:25px;"></div></a></td>'

        tableArray.push(comment);
        tableArray.push('</tr>');
    }
    $('#connectorStatusTable').append('<tbody>' + tableArray.join('') + '</tbody>');

    ShowLoading(false);
    ConfigureTooltips('');
    $('.ticket-comment').on('input', function () {
        $(this).addClass("comment-changed");
    });

    $('#number-active').html($('#connectorStatusTable > tbody > tr').length);
    $('#number-total').html(buildData.length);
    $('#buildcount').show();

}
///////


function FilterData(data, stageValues, andOr) {

    if (stageValues.length === 0)
        return data;

    console.log(JSON.stringify(stageValues));
    console.log(andOr);
    var newData = [];
    if (andOr.toLowerCase() === 'or') {

        for (var d in data) {

            var added = false;

            for (var s in stageValues) {

                if (!added) {

                    var stage = stageValues[s].stage;
                    var validValues = stageValues[s].statuses;

                    if (stage.toLowerCase() === 'client') {

                        if (validValues.indexOf(data[d].ParentTicket.Client) !== -1) {
                            newData.push(data[d]);
                            added = true;
                        }
                    }
                    else {

                        if (typeof (data[d].StageColors[stage]) !== "undefined" && data[d].StageColors[stage] !== null) {
                            if (validValues.indexOf(data[d].StageColors[stage].Status) !== -1) {
                                newData.push(data[d]);
                                added = true;
                            }
                        }
                    }
                }

            }
        }
    }
    else {
        //Run through each stage in serial.
        newData = data;

        for (var s in stageValues) {

            var singleStageArray = [stageValues[s]];

            newData = FilterData(newData, singleStageArray, 'or');
        }
    }

    return newData;
}

function ApplyFilters() {

    ShowLoading(true);

    var clientStage = 'client';
    var clientList = [];
    //Filter data by client first to reduce number of builds that we have to loop through entirely.
    $('#client-list').find('input').each(function(){
        if (this.checked) {
            clientList.push(this.id)
        }
    });

    var filteredByClient = FilterData(buildData, [{ stage: clientStage, statuses: clientList }], 'or');

    var useQuickFilter = false;

    //Quick Filters
    $('.quick-filter').each(function () {
        if (this.checked) {
            useQuickFilter = true;
            var filterParams = FindQuickFilter(this.id);
            ShowBuilds(FilterData(filteredByClient, filterParams.stageValues, filterParams.andOr));
        }
    });

    //Manually created stage filters.
    if (!useQuickFilter) {
        var stageValues = [];

        var selectAll = $('#cb-check-all')[0].checked;
        if (selectAll) {
            ShowBuilds(FilterData(filteredByClient, stageValues, andOr));
        }
        else {
            var rowData = $('#status-table > tbody > tr');
            var statusFromHeader = $('#status-table > thead').find('div > span')

            rowData.each(function (index, element) {

                var stage;
                var statuses = [];

                $(this).find('td').each(function (index, element) {
                    if (index === 0) {
                        stage = $(this).text();
                    }
                    else if (index > 1) {
                        var checkbox = $(this).find('input')[0];
                        var checked = checkbox.checked;
                        if (checked) {
                            statuses.push($(statusFromHeader[index - 2]).text()); //Two extra columns without headers.
                        }
                    }
                });
                if (statuses.length > 0)
                    stageValues.push({ stage: stage, statuses: statuses });
            });

            var andOr = $('#AND')[0].checked ? "and" : "or";

            ShowBuilds(FilterData(filteredByClient, stageValues, andOr));
        }
        
    }
}

function BuildFilters() {

    var i = 0;

    //Get full list of clients, display in filter modal.
    for (var d in buildData) {
        var client = buildData[d].ParentTicket.Client;
        if (typeof (clients[client]) === "undefined" || clients[client] === null) {
            clients[client] = true;

            var cb = '<div class="col s3"><input type="checkbox" checked="checked" id="' + client + '" /><label for="' + client + '">' + client + '</label></div>'

            if (i % 4 === 0) {
                $('#client-list').append('<div class="client-row row" id="client-row-' + Math.floor(i / 4) + '">' + cb + '</div>');
            }
            else {
                $('#client-row-' + Math.floor(i / 4)).append(cb);
            }
            i++; 
        }
    }

    //Build stage-status grid.
    for (var s in stages) {
        var thisStage = stages[s];
        var row = $('<tr id="' + thisStage.toLowerCase().split(' ').join('-') + '" />');
        $('#status-table').append(row);
        row.append('<td class="no-paddingz">' + thisStage + '</td>');
        for (var i = 0; i < $('#status-table > thead > tr > th').length - 1; i++) {
            row.append('<td class="no-paddingz"><div style="height:30px"><input type="checkbox" checked="checked" class="cb-' + i + '" value="' + thisStage + '" id="cb-' + thisStage.toLowerCase().split(' ').join('-') + i + '"/><label for="cb-' + thisStage.toLowerCase().split(' ').join('-') + i + '"/></div></td>');
        }
    }
    
    //Create quick-filters.
    
    $.getJSON('/Content/JSON/QuickFilters.json', null, function (data) {

        qf = data;
        $('#quick-filters').children().remove();

        for (var t in qf.types) {
            $('#quick-filters')
                .append(('<div class="col s6"><h6>' + qf.types[t] + '</h6><ul id="' + qf.types[t].split(' ').join('-') + '"/></div>'));
        }
        for (var f in qf.filters) {
            if (qf.types.indexOf(qf.filters[f].type) > -1) {

                var name = qf.filters[f].name;

                $('#' + qf.filters[f].type.split(' ').join('-'))
                    .append('<li><div><input class="quick-filter" name="filterGroup" type="radio" id="' +
                    name + '" /><label for="' + name +
                    '"><a class="tooltipped" data-position="right" data-delay="50" data-tooltip="' + qf.filters[f].description + '" value="' + name + '">' + name +
                    '</a></label></div></li>');
            }
        }
        ConfigureTooltips('#quick-filters');
    });

}

function ConfigureFilterClickActions() {
    //Region checkbox actions. On click, if any region is selected, disable client selection and select the clients in that region.
    $('.cb-group').click(function () {
        var anyChecked = false;
        $('.cb-group').each(function () {
            if (this.checked) {
                anyChecked = true;
            }
            var key = this.id.toLowerCase().split(' ').join('');
            var clients = regions[key];

            for (var c in clients) {
                var client = $('#' + clients[c])[0];
                if (typeof (client) !== "undefined" && client !== null) {
                    client.checked = this.checked;
                }
            }
        });

        if (anyChecked) {
            $('#client-list').find('input').attr('disabled', 'disabled');
        }
        else {
            $('#client-list').find('input').removeAttr('disabled');
        }

    });

    //Select all clients.
    $('#select-all-clients').click(function () {
        var checked = this.checked;
        $('.cb-group').each(function () {
            this.checked = !checked;
            this.click();
        });
    });

    //Row-level checkbox action for all or none. 
    $('.cb-0').click(function () {
        var checked = this.checked;
        $(this).closest('tr').find('input').each(function () {
            if (!$(this).hasClass('cb-0')) {
                this.checked = checked;
            }
        });
    });

    //Master select/deselect all for entire table.
    $('#cb-check-all').click(function () {
        var checked = this.checked;
        $('.cb-0').each(function () { this.checked = !checked; this.click(); })
    });


    $('#clear-quick-filters').click(function () {
        $('#quick-filters').find('input').each(function () { this.checked = false; });
        $('#status-list').find('input').removeAttr("disabled");
    });

    $('.quick-filter').change(function () {
        $('#status-list').find('input').attr("disabled", "disabled");
    });

    //Initialize by setting all checked. 
    $('#cb-check-all')[0].checked = false;
    $('#cb-check-all').click();
}

function BuildTooltippedTd(key, toolTipText, text) {
    return '<td class="no-paddingz">' +
            '<a href="https://jira.arcadiasolutions.com/browse/' + key +
            '" target="_blank" class="tooltipped" data-position="bottom" data-delay="50" data-tooltip="' +
            toolTipText + '">' +
            text + '</a></td>';
}

function ConfigureTooltips(prefix) {
    $(prefix + ' .tooltipped').tooltip();
}

function ShowLoading(loading) {
    if (loading) {
        $('#connectorStatusTable > tbody').remove()
        $('#pre-loader').show();
        $('.btn-floating').hide();
        $('#buildcount').hide();
    }
    else {
        $('#pre-loader').hide();
        $('.btn-floating').show();
    }
}

function FindQuickFilter(name) {
    for (var i = 0, len = qf.filters.length; i < len; i++) {
        if(qf.filters[i].name === name)
            return qf.filters[i];
    }
}

function SubmitComments() {

    $('.comment-changed').each(function () {
        var key = this.id;
        var comment = this.value;
        $.ajax({
            type: "POST",
            url: '/ConnectorStatus/SubmitComment',
            data: JSON.stringify({ 'key': key, 'comment': comment }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                Materialize.toast('Posted comment on ' + key + '.', 4000)
                $(this).removeClass('comment-changed');
            },
            error: function () {
            }
        });


    });
}

function FormatDate(date) {

    if (date === null) {
        return '';
    }
    return date.toString().substring(5, 7) + '/' + date.toString().substring(8, 10) + '/' + date.toString().substring(0, 4);
}

function FormatDecimal(n) {

    if (n === null) {
        return '';
    }
    return n.toFixed(2);
}


$(document).ready(main);