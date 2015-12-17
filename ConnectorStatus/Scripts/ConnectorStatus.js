var main = function () {

    $('#loadTickets').click(function () {
        
        $(this).removeClass('btn-primary')
        $(this).addClass('btn-info');
        $(this).prop('value','Updating tickets...');
        $('*').fadeTo(2, 0.9)
        $('#spinner').show();
    });

    $('#submitComments').click(function () {

        $(this).removeClass('btn-primary')
        $(this).addClass('btn-info');
        $(this).prop('value', 'Submitting Comments...');
        $('*').fadeTo(2, 0.9)
        $('#spinner').show();
    });

    //$('#connectorStatusTable').dataTable();

    
}

$(document).ready(main);
