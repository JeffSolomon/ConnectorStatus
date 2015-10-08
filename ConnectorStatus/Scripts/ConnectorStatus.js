var main = function () {

    $('td').click(function () {
        $(this).scrollLeft(10);

    });

    $('#loadTickets').click(function () {
        
        $(this).removeClass('btn-primary')
        $(this).addClass('btn-info');
        $(this).prop('value','Updating tickets...');
        $('*').fadeTo(2, 0.9)
        $('#spinner').show();
    });

}

$(document).ready(main);
