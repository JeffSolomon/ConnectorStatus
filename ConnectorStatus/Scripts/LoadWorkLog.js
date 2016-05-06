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

                    chart.tooltip(function (key, x, y, e, graph) {
                        return '<p><strong>' + key + '</strong></p>' +
                        '<p>' + y + ' in the month ' + x + '</p>';
                    });

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
        }
    });
}

function getEffortAndDurationData() {
    $('#chart2Spinner').show();
    $('#chart2chart').hide();

    $.ajax({
        type: "POST",
        url: '/WorkLogs/GetTicketEffortAndDuration',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            bubbleData = data;
            drawBubbles(data)
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

            chart.yAxis
                .tickFormat(d3.format(',.2f'))
                .axisLabel('Duration (Days)');

            chart.xAxis.axisLabel('Stage');

            d3.select('#chart2 svg')
                .datum(data)
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

function getTodaysDateString() {
    var todayDate = new Date();
    var todayDateString = (todayDate.getMonth() + 1) + '/' + todayDate.getDate() + '/' + todayDate.getFullYear();
    return todayDateString;

}



$(document).ready(main);