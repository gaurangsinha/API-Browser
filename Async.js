$(function () {
    $('form').submit(function () {

        var inputs = [];
        $(':input[type=text]', this).each(function () {
            inputs.push(this.name + '=' + escape(this.value));
        })

        var responseBox = $("#" + this.attributes['id'].value + '_response');

        // now if I join our inputs using '&' we'll have a query string
        jQuery.ajax({
            data: inputs.join('&'),
            url: this.action,
            type: this.method,
            error: function () {
                console.log("Failed to submit");
            },
            success: function (r) {
                responseBox.attr('style', 'display: block');
                var responseBody = $("#" + responseBox.attr("id") + "_body");
                responseBody.html('<pre>' + r + '</pre>');
                //alert(r);
            }
        }) // checkout http://jquery.com/api for more syntax and options on this method.

        // by default - we'll always return false so it doesn't redirect the user.
        return false;
    })
})