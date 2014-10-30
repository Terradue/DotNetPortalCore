/**
	the script only works on "input [type=text]"

**/

// don't declare anything out here in the global namespace

(function($) { // create private scope (inside you can use $ instead of jQuery)

    // functions and vars declared here are effectively 'singletons'.  there will be only a single
    // instance of them.  so this is a good place to declare any immutable items or stateless
    // functions.  for example:
	var today = new Date(); // used in defaults
    var months = 'January,February,March,April,May,June,July,August,September,October,November,December'.split(',');
	var monthlengths = '31,28,31,30,31,30,31,31,30,31,30,31'.split(',');
  	//var dateRegEx =  /^\d{1,2}\/\d{1,2}\/\d{2}|\d{4}$/;
	var dateRegEx = /^\d{4}-\d{2}-\d{2}$/;
	var yearRegEx = /^\d{4,4}$/;
	var calClickOutsideHandler = function() {
//		alert('click outside');
		$(".datepicker").find("span.close").click();
	};


    // next, declare the plugin function
    $.fn.simpleDatepicker = function(options) {

        // functions and vars declared here are created each time your plugn function is invoked

        // you could probably refactor your 'build', 'load_month', etc, functions to be passed
        // the DOM element from below

		var opts = jQuery.extend({}, jQuery.fn.simpleDatepicker.defaults, options);
		
		// replaces a date string with a date object in opts.startdate and opts.enddate, if one exists
		// populates two new properties with a ready-to-use year: opts.startyear and opts.endyear
		
		setupYearRange();
		/** extracts and setup a valid year range from the opts object **/
		function setupYearRange () {
			
			var startyear, endyear;  
			if (opts.startdate.constructor == Date) {
				startyear = opts.startdate.getFullYear();
			} else if (opts.startdate) {
				if (yearRegEx.test(opts.startdate)) {
				startyear = opts.startdate;
				} else if (dateRegEx.test(opts.startdate)) {
					opts.startdate = new Date(opts.startdate.split("-")[0],opts.startdate.split("-")[1]-1,opts.startdate.split("-")[2]);
					startyear = opts.startdate.getFullYear();
				} else {
				startyear = today.getFullYear();
				}
			} else {
				startyear = today.getFullYear();
			}
			opts.startyear = startyear;
			
			if (opts.enddate.constructor == Date) {
				endyear = opts.enddate.getFullYear();
			} else if (opts.enddate) {
				if (yearRegEx.test(opts.enddate)) {
					endyear = opts.enddate;
				} else if (dateRegEx.test(opts.enddate)) {
					opts.enddate = new Date(opts.enddate.split("-")[0],opts.enddate.split("-")[1]-1,opts.enddate.split("-")[2]);
					endyear = opts.enddate.getFullYear();
				} else {
					endyear = today.getFullYear();
				}
			} else {
				endyear = today.getFullYear();
			}
			opts.endyear = endyear;	
		}
		
		/** HTML factory for the actual datepicker table element **/
		// has to read the year range so it can setup the correct years in our HTML <select>
		function newDatepickerHTML () {
			
			var years = [];
			
			// process year range into an array
			for (var i = 0; i <= opts.endyear - opts.startyear; i ++) years[i] = opts.startyear + i;
	
			// build the table structure
			var table = jQuery('<table class="datepicker" cellpadding="0" cellspacing="0"></table>');
			table.append('<thead></thead>');
			table.append('<tfoot></tfoot>');
			table.append('<tbody></tbody>');
			
				// month select field
				var monthselect = '<select name="month" style="width:70px">';
				for (var i=0; i<months.length;++i )monthselect += '<option value="'+i+'">'+months[i]+'</option>';
				//for (var i in months) monthselect += '<option value="'+i+'">'+months[i]+'</option>';
				monthselect += '</select>';
			
				// year select field
				var yearselect = '<select name="year">';
				//for (var index = 0, len = myArray.length; index < len; ++index) {
				for (var i=0; i<years.length;++i )yearselect += '<option>'+years[i]+'</option>';
				//for (var i in years) yearselect += '<option>'+years[i]+'</option>';
				
				yearselect += '</select>';
			
			jQuery("thead",table).append('<tr class="controls"><th colspan="7"><span class="prevMonth">&laquo;</span>&nbsp;'+monthselect+yearselect+'&nbsp;<span class="nextMonth">&raquo;</span></th></tr>');
			jQuery("thead",table).append('<tr class="days"><th>S</th><th>M</th><th>T</th><th>W</th><th>T</th><th>F</th><th>S</th></tr>');
			jQuery("tfoot",table).append('<tr><td colspan="2"><span class="today">today</span></td><td colspan="3">&nbsp;</td><td colspan="2"><span class="close">close</span></td></tr>');
			for (var i = 0; i < 6; i++) jQuery("tbody",table).append('<tr><td></td><td></td><td></td><td></td><td></td><td></td><td></td></tr>');	
			return table;
		}
		
		/** get the real position of the input (well, anything really) **/
		//http://www.quirksmode.org/js/findpos.html
		function findPosition (obj) {
			var curleft = curtop = 0;
			if (obj.offsetParent) {
				do { 
					curleft += obj.offsetLeft;
					curtop += obj.offsetTop;
				} while (obj = obj.offsetParent);
				return [curleft,curtop];
			} else {
				return false;
			}
		}
		
		/** load the initial date and handle all date-navigation **/
		// initial calendar load (e is null)
		// prevMonth & nextMonth buttons
		// onchange for the select fields
		function loadMonth (e, el, datepicker, chosendate) {
			
			// reference our years for the nextMonth and prevMonth buttons
			var mo = jQuery("select[name=month]", datepicker).get(0).selectedIndex;
			var yr = jQuery("select[name=year]", datepicker).get(0).selectedIndex;
			var yrs = jQuery("select[name=year] option", datepicker).get().length;
			
			// first try to process buttons that may change the month we're on
			if (e && jQuery(e.target).hasClass('prevMonth')) {				
				if (0 == mo && yr) {
					yr -= 1; mo = 11;
					jQuery("select[name=month]", datepicker).get(0).selectedIndex = 11;
					jQuery("select[name=year]", datepicker).get(0).selectedIndex = yr;
				} else {
					mo -= 1;
					jQuery("select[name=month]", datepicker).get(0).selectedIndex = mo;
				}
			} else if (e && jQuery(e.target).hasClass('nextMonth')) {
				if (11 == mo && yr + 1 < yrs) {
					yr += 1; mo = 0;
					jQuery("select[name=month]", datepicker).get(0).selectedIndex = 0;
					jQuery("select[name=year]", datepicker).get(0).selectedIndex = yr;
				} else { 
					mo += 1;
					jQuery("select[name=month]", datepicker).get(0).selectedIndex = mo;
				}
			}
			
			// maybe hide buttons
			if (0 == mo && !yr) jQuery("span.prevMonth", datepicker).hide(); 
			else jQuery("span.prevMonth", datepicker).show(); 
			if (yr + 1 == yrs && 11 == mo) jQuery("span.nextMonth", datepicker).hide(); 
			else jQuery("span.nextMonth", datepicker).show(); 
			
			// clear the old cells
			var cells = jQuery("tbody td", datepicker).unbind().empty().removeClass('date');
			
			// figure out what month and year to load
			var m = jQuery("select[name=month]", datepicker).val();
			var y = jQuery("select[name=year]", datepicker).val();
			var d = new Date(y, m, 1);
			var startindex = d.getDay();
			var numdays = monthlengths[m];
			
			// http://en.wikipedia.org/wiki/Leap_year
			if (1 == m && ((y%4 == 0 && y%100 != 0) || y%400 == 0)) numdays = 29;
			
			// test for end dates (instead of just a year range)
			if (opts.startdate.constructor == Date) {
				var startMonth = opts.startdate.getMonth();
				var startDate = opts.startdate.getDate();
			}
			if (opts.enddate.constructor == Date) {
				var endMonth = opts.enddate.getMonth();
				var endDate = opts.enddate.getDate();
			}
			
			// walk through the index and populate each cell, binding events too
			for (var i = 0; i < numdays; i++) {
			
				var cell = jQuery(cells.get(i+startindex)).removeClass('chosen');
				
				// test that the date falls within a range, if we have a range
				if ( 
					(yr || ((!startDate && !startMonth) || ((i+1 >= startDate && mo == startMonth) || mo > startMonth))) &&
					(yr + 1 < yrs || ((!endDate && !endMonth) || ((i+1 <= endDate && mo == endMonth) || mo < endMonth)))) {
				
					cell
						.text(i+1)
						.addClass('date')
						.hover(
							function () { jQuery(this).addClass('over'); },
							function () { jQuery(this).removeClass('over'); })
						.click(function () {
							var chosenDateObj = new Date(jQuery("select[name=year]", datepicker).val(), jQuery("select[name=month]", datepicker).val(), jQuery(this).text());
							closeIt(el, datepicker, chosenDateObj);
						});
						
					// highlight the previous chosen date
					if (i+1 == chosendate.getDate() && m == chosendate.getMonth() && y == chosendate.getFullYear()) cell.addClass('chosen');
				}
			}
		}
		
		/** closes the datepicker **/
		// sets the currently matched input element's value to the date, if one is available
		// remove the table element from the DOM
		// indicate that there is no datepicker for the currently matched input element
		function closeIt (el, datepicker, dateObj) { 
			if (dateObj && dateObj.constructor == Date){
				el.val(jQuery.fn.simpleDatepicker.formatOutput(dateObj));
				el.change();
			}
			datepicker.remove();
			datepicker = null;
			jQuery.data(el.get(0), "simpleDatepicker", { hasDatepicker : false });
			$(document).unbind('click',calClickOutsideHandler);
		}

        // iterate the matched nodeset
        return this.each(function() {
			
            // functions and vars declared here are created for each matched element. so if
            // your functions need to manage or access per-node state you can defined them
            // here and use $this to get at the DOM element
			tt = $(this).find("input");
			if (( jQuery(tt).is('input') && 'text' == jQuery(tt).attr('type')) | ( jQuery(this).is('input') && 'text' == jQuery(this).attr('type'))){			
				var datepicker; 
				if (jQuery(this).is('input')){
					jQuery.data(jQuery(this).get(0), "simpleDatepicker", { hasDatepicker : false });
				}
				else{
					jQuery.data(jQuery(tt).get(0), "simpleDatepicker", { hasDatepicker : false });
					jQuery(tt).click(function(ev){
						return false;
					});
				}
				// open a datepicker on the click event
				jQuery(this).click(function (ev) {
					if (jQuery(this).is('input')){var $this = jQuery(ev.target);}
					else {var $this = jQuery(ev.target).find("input");}
					//alert($(".datepicker").length);
					if ($(".datepicker").length!=1) {
						ev.stopPropagation();						
						$(document).click(calClickOutsideHandler);	
					}
					
					if (false == jQuery.data($this.get(0), "simpleDatepicker").hasDatepicker) {
						// store data telling us there is already a datepicker
						jQuery.data($this.get(0), "simpleDatepicker", { hasDatepicker : true });
						
						// validate the form's initial content for a date
						var initialDate = $this.val();
						initialDate=initialDate.split("T")[0];
						if (initialDate && dateRegEx.test(initialDate)) {							
							var chosendate = new Date(initialDate.split("-")[0],initialDate.split("-")[1]-1,initialDate.split("-")[2]);
						} else if (opts.chosendate.constructor == Date) {
							var chosendate = opts.chosendate;
						} else if (opts.chosendate) {
							var chosendate = new Date(opts.chosendate.split("-")[0],opts.chosendate.split("-")[1]-1,opts.chosendate.split("-")[2]);
						} else {
							var chosendate = today;
						}

						// determinate start and end date

						opts.endyear=chosendate.getFullYear()+25;
						opts.startyear=chosendate.getFullYear()-25;
						if (opts.endyear>today.getFullYear()) opts.endyear=today.getFullYear();
							
						// insert the datepicker in the DOM
						datepicker = newDatepickerHTML();
						jQuery("body").prepend(datepicker);
						
						// position the datepicker
						var elPos = findPosition($this.get(0));
						var x = (parseInt(opts.x) ? parseInt(opts.x) : 0) + elPos[0];
						var y = (parseInt(opts.y) ? parseInt(opts.y) : 0) + elPos[1];
						jQuery(datepicker).css({ position: 'absolute', left: x, top: y });
						//chosendate=chosendate.split('-')[1]+"-"+chosendate.split('-')[2]+"-"+chosendate.split('-')[0];
						// bind events to the table controls
						jQuery("span", datepicker).css("cursor","pointer");
						jQuery("select", datepicker).click(function (e) { e.stopPropagation();});
						jQuery("select", datepicker).bind('change', function () { loadMonth (null, $this, datepicker, chosendate);});
						jQuery("span.prevMonth", datepicker).click(function (e) { loadMonth (e, $this, datepicker, chosendate);e.stopPropagation(); });
						jQuery("span.nextMonth", datepicker).click(function (e) { loadMonth (e, $this, datepicker, chosendate);e.stopPropagation();});
						jQuery("span.today", datepicker).click(function () { closeIt($this, datepicker, new Date()); });
						jQuery("span.close", datepicker).click(function () { closeIt($this, datepicker); });
						
						// set the initial values for the month and year select fields
						// and load the first month
						jQuery("select[name=month]", datepicker).get(0).selectedIndex = chosendate.getMonth();
						jQuery("select[name=year]", datepicker).get(0).selectedIndex = Math.max(0, chosendate.getFullYear() - opts.startyear);
						loadMonth(null, $this, datepicker, chosendate);
					}
					
				});
			}

        });

    };

    // finally, I like to expose default plugin options as public so they can be manipulated.  one
    // way to do this is to add a property to the already-public plugin fn

	jQuery.fn.simpleDatepicker.formatOutput = function (dateObj) {
		//return (dateObj.getMonth() + 1) + "/" + dateObj.getDate() + "/" + dateObj.getFullYear();	
		var rr=dateObj.getFullYear() + "-";
		if (dateObj.getMonth()<9) {rr+="0"}
		rr+=(dateObj.getMonth()+1) + "-";
		if (dateObj.getDate()<10) {rr+="0"}
		rr+=dateObj.getDate();
		return  rr
	};
	
	jQuery.fn.simpleDatepicker.defaults = {
		// date string matching /^\d{1,2}\/\d{1,2}\/\d{2}|\d{4}$/
		chosendate : today,
		
		// date string matching /^\d{1,2}\/\d{1,2}\/\d{2}|\d{4}$/
		// or four digit year
		startdate : today.getFullYear(), 
		enddate : today.getFullYear() + 1,
		
		// offset from the top left corner of the input element
		x : 18, // must be in px
		y : 18 // must be in px
	};

})(jQuery);
