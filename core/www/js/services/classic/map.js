/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/

// This code deals with the map widget on the service page
// It assumes that there is some html element of the class .dataset to link the results


// generic object used in the page
// to-do: they should be linked in some kind of objet ?
// to-do: check their use and file dependencies
var featuresStyle;
var selectedFeaturesStyle;
var selectedFeaturesLayer;
var auxFeaturesLayer;
var OSIconPanel;
var OSMap;
var OSLayer;		
var epsg4326 = new OpenLayers.Projection("EPSG:4326");

	$(document).ready(function() {
		// Link events to the dataset element 
		// these events are from the dataset -> map 
		// (define what happens to the map when an action is performed on the dataset list)
		// all relations should be done by the OS_feature_id of the element that is passed 

		// this defines the map behaviour when a feature is deleted
			$('.ifyOSMap').bind('deletefeatures', function(event, deletedOptions) {
				$(deletedOptions).each(function () {
					selectedFeaturesLayer.removeFeatures(selectedFeaturesLayer.getFeatureById( $(this).data('OS_feature_id') ));
				});
			});			

		// this defines the map behaviour when a feature is selected in the html select element (class dataset)
			$('.ifyOSMap').bind('selectfeatures', function(event, selectOptions) {
				$(selectOptions).each(function () {
					if ($(this).data('OS_feature_id')){
					selectControl.highlight(selectedFeaturesLayer.getFeatureById( $(this).data('OS_feature_id') ));
					}
				})
			});
			
		// this defines the map behaviour when a feature is unselected in the html select element (class dataset)
			$('.ifyOSMap').bind('unselectfeatures', function(event, selectOptions) {
				$(selectOptions).each(function () {
					if ($(this).data('OS_feature_id')){
					selectControl.unhighlight(selectedFeaturesLayer.getFeatureById( $(this).data('OS_feature_id') ));
					}
				})
			});

			$('#ifyServiceMap').html('');
			$('#paneldiv').html('');
			OpenLayers.ProxyHost="/proxy4.aspx?url="
	})


// this function defines the map controls
// it's separated from the onready code so that we can replace it in some future service 
// to-do: this should read from a configuration object
	function SetMapControls(selFeatureIndex, MapObj){
			auxFeaturesStyle = OpenLayers.Util.applyDefaults({
				fillColor: "#C68D84",
				fillOpacity:0.7,
				strokeOpacity:0.1,
				strokeWidth:3,
				strokeDashstyle:'longdash',
				strokeColor:"#953A2B",
				hoverFillColor:'white',
				hoverFillOpacity:0.7,
				strokeLinecap:'round',
				hoverStrokeColor:'red',
				hoverStrokeOpacity:1,
				hoverStrokeWidth:0.2,
				pointRadius:2,
				hoverPointRadius:1,
				hoverPointUnits:'%'}, OpenLayers.Feature.Vector.style["default"]);


			
			featuresStyle = OpenLayers.Util.applyDefaults({
				fillColor: "#C68D84",
				fillOpacity:0.3,
				strokeOpacity:0.1,
				strokeWidth:2,
				pointRadius:2,
				
				//strokeDashstyle:'dash',
				//cursor:'help',
			    strokeColor: "#953A2B"}, OpenLayers.Feature.Vector.style["default"]);

			selectedFeaturesStyle = OpenLayers.Util.applyDefaults({
				fillColor: "#C68D84",
				fillOpacity:0.3,
				strokeOpacity:0.8,
				strokeWidth:1,
				strokeDashstyle:'dash',
				//cursor:'help',
			    strokeColor: "#953A2B"}, OpenLayers.Feature.Vector.style["select"]);

			selectedFeaturesLayer = new OpenLayers.Layer.Vector("Orbit Layer", {
					displayInLayerSwitcher: false,
					//style : featuresStyle
					styleMap: new OpenLayers.StyleMap({
							"default": featuresStyle,
                            "select": selectedFeaturesStyle
                    })
			   }
			);

			MapObj.addLayer(selectedFeaturesLayer);
			
			auxFeaturesLayer = new OpenLayers.Layer.Vector("Auxiliary Features", {
					displayInLayerSwitcher: false,
					//style : featuresStyle
					styleMap: new OpenLayers.StyleMap({
							"default": auxFeaturesStyle,
                            "select": selectedFeaturesStyle
                    })
			   }
			);
			MapObj.addLayer(auxFeaturesLayer);

			selectControl = new OpenLayers.Control.SelectFeature(MapObj.layers[selFeatureIndex],
                {	multiple:true,
					clickout:true,
					toggle:true,
					box:true,
					hover:false,
					//hover:true,
					//highlightOnly:true,
					//selectStyle:selectedFeaturesStyle,
					//{strokeWidth:1,fillColor: "#C68D84",	fillOpacity:0.3,},
					onSelect: onFeatureSelect, 
					onUnselect: onFeatureUnselect
				}
			);
//			MapObj.addControl(selectControl);
//			selectControl.activate();   

			nav = new OpenLayers.Control.NavigationHistory();            
            MapObj.addControl(nav);
			MapObj.addControl( new OpenLayers.Control.Attribution());
			MapObj.addControl( new OpenLayers.Control.ScaleLine());
			MapObj.addControl( new OpenLayers.Control.MousePosition({displayProjection: new OpenLayers.Projection('EPSG:4326'),numDigits: 2}));	
//			MapObj.addControl( new OpenLayers.Control.MousePosition({displayProjection: MapObj.getProjectionObject()}));	
			
			
			if (MapObj.layers.length>3){
				MapObj.addControl(new OpenLayers.Control.LayerSwitcher({roundedCornerColor:"#40A150",roundedCorner:false}));
			}
			//MapObj.addControl( new OpenLayers.Control.KeyboardDefaults());
			
            OSIconPanel = new OpenLayers.Control.Panel({'div':OpenLayers.Util.getElement('paneldiv')});
            OSIconPanel.addControls([
				new OpenLayers.Control.ZoomToMaxExtent({title:"Zoom to the max extent"}), 
				new OpenLayers.Control.Navigation({zoomBoxEnabled:true}),
				//new OpenLayers.Control.MouseDefaults({title:'You can use the default mouse configuration'}), 
				selectControl,
				new OpenLayers.Control.ZoomIn({title:"Zoom In"}),
				new OpenLayers.Control.ZoomOut({title:"Zoom Out"}),
				new OpenLayers.Control.ZoomBox({title:"Zoom Box"}),
				nav.previous, nav.next
            ]);
//            if ($("#bbox").length>0) OSIconPanel.addControls([startSelectGeoBox(MapObj,"#bbox")]);
			 //OSMap.addControl(new OpenLayers.Control.LayerSwitcher());
			MapObj.addControl(OSIconPanel);
  //          if ($("#bbox").length>0) selectionBox.activate();
			
	}

// this function triggers an event when the feature is selected on the map
	function onFeatureSelect(feature) {
		$('.dataset').trigger('selectfeature',feature.attributes.identifier);

	}
		
// this function triggers an event when the feature is unselected on the map
	function onFeatureUnselect(feature) {
		$('.dataset').trigger('unselectfeature',feature.attributes.identifier);
	}
	
