/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/  


  var selectionBoxLayer;
  // moved to map.js    var selectedFeaturesLayer;
  var selectionBox;
  var InputGeoBoxName;
  // moved to map.js  var epsg4326;// = new OpenLayers.Projection("EPSG:4326");
  var ParentMapObj;


/**
 * Defines a control element for the definition of select box 
 * It needs as input the map where the selection is to be drawn 
 * and the element where the bbox values will be written 
 * @author		pedro
 * @param  MapObj	ref to the openlayers map 
 * @param  ElemName	name of the element(s) to write the value (this is in the css format: #bbox or .bbox)
 * @return returns the  new control 
*/
  function startSelectGeoBox(MapObj, ElemName) {
	//epsg4326 = new OpenLayers.Projection("EPSG:4326");
	InputGeoBoxName = ElemName;

	var selectStyle = OpenLayers.Util.applyDefaults({
	    fillColor: "#40A150",
		strokeColor: "orange", //"#40A150",
		fillOpacity:0.1,
		strokeOpacity:0.8,
		strokeWidth:2//,
		//strokeDashstyle:'dash'
		//cursor:'help',
		}, OpenLayers.Feature.Vector.style["select"]);

	selectionBoxLayer = new OpenLayers.Layer.Vector("Selection BBox Layer", {
      displayInLayerSwitcher: false,
	  style : selectStyle
    });
    MapObj.addLayer(selectionBoxLayer);
    selectionBox = new OpenLayers.Control.DrawFeature(selectionBoxLayer, OpenLayers.Handler.RegularPolygon, { 
      handlerOptions: {
        sides: 4,
        irregular: true,
        persist: true,		
        callbacks: {done: endDrag }
	  }
    });
	//MapObj.addControl(selectionBox);
	//PanelObj.addControls([selectionBox])
 	ParentMapObj = MapObj;
	$(InputGeoBoxName).change(boundsChanged);
	//$(InputGeoBoxName)[0].onchange=boundsChanged;
	return selectionBox;
  }



/** internal functions */
  function boundsChanged() {
	var bbox=$(InputGeoBoxName).val().split(",");
    var bounds = new OpenLayers.Bounds(bbox[0],bbox[1],bbox[2],bbox[3]);
    clearBox();
    drawBox(bounds);
    validateControls();
  }


  function endDrag(bbox) {
    var bounds = bbox.getBounds();
    setBounds(bounds);
    selectionBox.deactivate();
	//selectionBox.activate();

  }

  function setBounds(bounds) {
    
    var decimals = Math.pow(10, Math.floor(ParentMapObj.getZoom() / 3));
    bounds = bounds.transform(ParentMapObj.getProjectionObject(), new OpenLayers.Projection("EPSG:4326"));
    var minlon = Math.round(bounds.left * decimals) / decimals;
    var minlat= Math.round(bounds.bottom * decimals) / decimals;
    var maxlon = Math.round(bounds.right * decimals) / decimals;
    maxlat = Math.round(bounds.top * decimals) / decimals;
	$(InputGeoBoxName).val(minlon +',' + minlat +',' + maxlon +',' + maxlat);
	$(InputGeoBoxName).change();
  }

  function clearBox() {
    selectionBoxLayer.destroyFeatures();
  }

  function drawBox(bounds) {
     clearBox();
	 var box = bounds.toGeometry();
	 box.transform(new OpenLayers.Projection("EPSG:4326"), ParentMapObj.getProjectionObject());
 	 var feature = new OpenLayers.Feature.Vector(box);
     selectionBoxLayer.addFeatures(feature);
  }

  function validateControls() {
    var bounds = new OpenLayers.Bounds($("minlon").value, $("minlat").value, $("maxlon").value, $("maxlat").value);
	/*
	to-do we can use this for the maxing bounds
    if (bounds.getWidth() * bounds.getHeight() > 0.25) {
     
    }
 	*/
  }

  