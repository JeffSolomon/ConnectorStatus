var bubbleData = [];

var main = function () {

    $('body').addClass('grey lighten-4');

    $('#no-data-warning').hide();

    $('.date-input').bind('input', function () {
        var stringDate = this.value;

        if (Date.parse(stringDate)) {
            $('#GetData').prop('disabled', false);
        }
        else {
            $('#GetData').prop('disabled', true);
        }
    });

    $('#start-date').val('1/1/2016');
    $('#end-date').val(getTodaysDateString());


    $('#GetData').click(function(){
        getData($('#start-date').val(), $('#end-date').val());
    });

    $('#GetData').click();

    getEffortAndDurationData();
};


function getData(start, end) {
    $('#chart1Spinner').show();
    $('#chart1chart').hide();

    $.ajax({
        type: "POST",
        url: '/WorkLogs/GetClientStageData',
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ 'startDate': start, 'endDate': end }),
        dataType: "json",
        success: function (data) {
            if (data) {
                nv.addGraph(function () {
                    var chart = nv.models.multiBarChart()
                        .x(function (d) { return d.stage })
                        .y(function (d) { return d.hours })
                        .margin({ top: 30, right: 40, bottom: 150, left: 60 })
                        .stacked(true)
                        .rotateLabels(45)
                        .reduceXTicks(false)
                        .width(650)
                        .height(600)
                        //.tooltips(false)
                        .showControls(false);

                    chart.yAxis
                        .tickFormat(d3.format(',.2f'));

                    d3.select('#chart1 svg')
                        .datum(data)
                      .transition().duration(15000)
                        .call(chart);

                    nv.utils.windowResize(chart.update);

                    $('#chart1Spinner').hide();
                    $('#chart1chart').show();

                    return chart;
                })
            }
            else {
                $('#no-data-warning').show();
            }
        },
        error: function () {
            window.location.href = '/';
        }
    });
}

function getEffortAndDurationData() {
    $('#chart2Spinner').show();
    $('#chart2chart').hide();
    $('#chart3Spinner').show();
    $('#chart3chart').hide();

    $.ajax({
        type: "POST",
        url: '/WorkLogs/GetTicketEffortAndDuration',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            bubbleData = data;
            drawBubbles(data);
            drawBubbles2(data);
        }
    });
}

function filterByKeyWord(keyword) {
    var newData = [];
    for (var i = 0; i < bubbleData.length; i++) {
        if (bubbleData[i].key.indexOf(keyword) > -1) {
            newData.push(bubbleData[i]);
        }  
    }
    drawBubbles(newData);
}

function drawBubbles(data) {
    if (data) {
        
        nv.addGraph(function () {
            var chart = nv.models.scatterChart()
                    .showLegend(false)
                    .showDistX(true)    //showDist, when true, will display those little distribution lines on the axis.
                    .showDistY(true)
                    .width(700)
                    .height(600)
                    .color(d3.scale.category10().range());

            //var xAxisMap = new Array();
            //xAxisMap[1] = 'Kick off';
            //xAxisMap[2] = 'Client Access';
            //xAxisMap[3] = 'Requirements';
            //xAxisMap[4] = 'Environment Build';
            //xAxisMap[5] = 'Extract and Load';
            //xAxisMap[6] = 'Query Development';
            //xAxisMap[7] = 'Code Review"}';
            //xAxisMap[8] = 'Seed Data Prep';
            //xAxisMap[9] = 'Clinical Review';
            //xAxisMap[10] = 'DQA';
            //xAxisMap[11] = 'Verification';
            //xAxisMap[12] = 'Deliver to QA';
            //xAxisMap[13] = 'SIT Prep';
            //xAxisMap[14] = 'SIT Execute';
            //xAxisMap[15] = 'UAT Prep';
            //xAxisMap[16] = 'UAT Execute';
            //xAxisMap[17] = 'Go-Live Approval';
            //xAxisMap[18] = 'Go-Live';

            chart.tooltip.contentGenerator(function (d) {
                console.log(d);
                return '<table>' +
                        '<tr>' +
                            '<td>Connector</td>' +
                            '<td><b>' + d.series[0].key + '</b></td>' +
                        '</tr>' +
                        '<tr>' +
                            '<td>Stage</td>' +
                            '<td><b>' + d.point.label + '</b></td>' +
                        '</tr>' +
                        '<tr>' + 
                            '<td>Duration</td>' + 
                            '<td><b>' + d.point.y.toFixed(2) + '</b> Days</td>' + 
                        '</tr>' +
                        '<tr>' + 
                            '<td>Effort</td>' + 
                            '<td><b>' + d.point.size.toFixed(2) + '</b> Hours</td>' +
                        '</tr>' +
                     '</table>';
            });

            chart.yAxis
                .tickFormat(d3.format(',.2f'))
                .axisLabel('Duration (Days)');

            chart.xAxis.tickFormat(function (d) { return ''; });

            var finalData = mapToBubbleData(data);

            d3.select('#chart2 svg')
                .datum(finalData)
                .transition().duration(15000)
                .call(chart);

            nv.utils.windowResize(chart.update);

            $('#chart2Spinner').hide();
            $('#chart2chart').show();

            return chart;
        })
    }
    else {
        $('#no-data-warning').show();
    }
}

function drawBubbles2(data) {
    if (data) {

        nv.addGraph(function () {
            var chart = nv.models.scatterChart()
                    //.showLegend(false)
                    .showDistX(true)    //showDist, when true, will display those little distribution lines on the axis.
                    .showDistY(true)
                    .width(700)
                    .height(600)
                    .color(d3.scale.category10().range())
                    .xScale(d3.scale.log())
                    .yScale(d3.scale.log());

            chart.tooltip.contentGenerator(function (d) {
                console.log(d);
                return '<p><b>' + d.point.label + '</b></p>' +
                    '<table>' +    
                        '<tr>' +
                            '<td>Duration</td>' +
                            '<td><b>' + d.point.y.toFixed(2) + '</b> Days</td>' +
                        '</tr>' +
                        '<tr>' +
                            '<td>Effort</td>' +
                            '<td><b>' + d.point.x.toFixed(2) + '</b> Hours</td>' +
                        '</tr>' +
                     '</table>';
            });

            chart.yAxis
                .tickFormat(function (d) { return ''; })
                .axisLabel('Duration (Days)');

            //chart.forceY([1, 100]);

            chart.xAxis
                .tickFormat(function (d) { return ''; })
                .axisLabel('Effort (Hours)');

            var finalData = mapToBubbleData2(data);

            d3.select('#chart3 svg')
                .datum(finalData)
                .transition().duration(15000)
                .call(chart);

            nv.utils.windowResize(chart.update);

            $('#chart3Spinner').hide();
            $('#chart3chart').show();

            return chart;
        })
    }
    else {
        $('#no-data-warning').show();
    }
}

function getTodaysDateString() {
    var todayDate = new Date();
    var todayDateString = (todayDate.getMonth() + 1) + '/' + todayDate.getDate() + '/' + todayDate.getFullYear();
    return todayDateString;

}

function mapToBubbleData(rawData) {

    var data = new Array();
    for (var i = 0; i < rawData.length; i++) {
        var subData = new Array();
        for (var j = 0; j < rawData[i].values.length; j++) {
            subData.push( {
                x: rawData[i].values[j].StageNumber,
                y: rawData[i].values[j].Duration,
                size: rawData[i].values[j].Effort,
                label: rawData[i].values[j].StageLabel
            })
        }
        data.push({ key : rawData[i].key, values : subData});
    }

    return data;

}

function mapToBubbleData2(rawData) {

    var data = {};

    for (var i = 0; i < rawData.length; i++) {
        for (var j = 0; j < rawData[i].values.length; j++) {
            if (!data[rawData[i].values[j].StageLabel]) {
                data[rawData[i].values[j].StageLabel] = {
                    key: rawData[i].values[j].StageLabel,
                    value: new Array()
                };
            }
            data[rawData[i].values[j].StageLabel]
                .value
                .push({
                    x: rawData[i].values[j].Effort,
                    y: rawData[i].values[j].Duration,
                    size: 2,//rawData[i].values[j].Effort,
                    label: rawData[i].key + ' - ' + rawData[i].values[j].StageLabel
                });    
        }
    }

    var finalData = new Array();
    for (d in data) {
        finalData.push({ key: data[d].key, values: data[d].value })
    }

    return finalData;

}



$(document).ready(main);