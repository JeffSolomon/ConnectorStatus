var main = function() {

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

    $('#start-date').val('1/1/2015');
    $('#end-date').val(getTodaysDateString());


    $('#GetData').click(function(){
        getData($('#start-date').val(), $('#end-date').val());
    });

    $('#GetData').click();

    
};

function getData(start, end) {
    $.ajax({
        type: "POST",
        url: '/WorkLogs/GetClientStageData',
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ 'startDate': start, 'endDate': end }),
        dataType: "json",
        success: function (data) {
            if (data) {
                nv.addGraph(function () {
                    var chart = nv.models.multiBarHorizontalChart()
                        .x(function (d) { return d.stage })
                        .y(function (d) { return d.hours })
                        .margin({ top: 30, right: 20, bottom: 50, left: 175 })
                         .stacked(true)
                        //.tooltips(false)
                        .showControls(true);

                    chart.yAxis
                        .tickFormat(d3.format(',.2f'));

                    d3.select('#chart svg')
                        .datum(data)
                      .transition().duration(15000)
                        .call(chart);

                    nv.utils.windowResize(chart.update);

                    return chart;
                })
            }
            else {
                $('#no-data-warning').show();
            }
        }
    });
}

function getTodaysDateString() {
    var todayDate = new Date();
    var todayDateString = (todayDate.getMonth() + 1) + '/' + todayDate.getDate() + '/' + todayDate.getFullYear();
    return todayDateString;

}



$(document).ready(main);