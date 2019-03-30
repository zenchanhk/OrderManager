
package test;
//import ibcontroller.;

import java.lang.reflect.Field;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Collections;

import java.util.List;
import java.nio.file.Path;
// imports
import org.deeplearning4j.nn.modelimport.keras.KerasModelImport;
import org.deeplearning4j.nn.multilayer.MultiLayerNetwork;
import org.nd4j.linalg.api.ndarray.INDArray;
import org.nd4j.linalg.factory.Nd4j;
import org.nd4j.linalg.io.ClassPathResource;

import org.opencv.core.*;
import org.opencv.imgcodecs.Imgcodecs;
import org.opencv.imgproc.Imgproc;



public class test{

    public static void main(String[] args) {
        try {
            System.setProperty("java.library.path", "c:\\ibcontroller");
            Field fieldSysPath = ClassLoader.class.getDeclaredField("sys_paths");
            fieldSysPath.setAccessible(true);
            fieldSysPath.set(null, null);
        } catch (Exception ex) {

        }
        // load OpenCV library
        System.loadLibrary("opencv_java345");

        String filename = "C:\\IBController\\images\\15537782746671.png";
        String re = captchaDecoder(filename);
    }

    static String captchaDecoder(String img_path) {
        try {
            // load the model
            String simpleMlp = Paths.get("c:\\ibcontroller\\captcha_model.hdf5").toString();
            MultiLayerNetwork model = KerasModelImport.
                    importKerasSequentialModelAndWeights(simpleMlp);

            // read image
            Mat img = Imgcodecs.imread(img_path, Imgcodecs.CV_LOAD_IMAGE_GRAYSCALE);

            // get image height to validate contour later
            int org_h = img.height();
            Mat img1 = new Mat();
            Mat thresh = new Mat();
            Core.copyMakeBorder(img, img1, 0, 0, 0, 0, Core.BORDER_REPLICATE);
            Imgproc.threshold(img1, thresh, 0, 255, Imgproc.THRESH_BINARY_INV | Imgproc.THRESH_OTSU);

            // get contours
            List<MatOfPoint> contours = new ArrayList<>();
            Mat hierarchy = new Mat();
            Imgproc.findContours(thresh.clone(), contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            StringBuilder sb = new StringBuilder();

            List<Rect> regions = new ArrayList<>();
            for (MatOfPoint point: contours) {
                Rect rect =Imgproc.boundingRect(point);
                if (rect.y != 0 && rect.height <= org_h) {
                    regions.add(rect);
                } else {
                    //return sb.toString();
                }
            }
            // sort regions
            Collections.sort(regions, (o1, o2) -> o1.x > o2.x ? 1 : o1.x == o2.x ? 0 : -1);
            for (Rect region: regions) {
                //Mat tmp = new Mat(img1, region);
                //Imgcodecs.imwrite("c:\\ibcontroller\\1.png", tmp);
                /*
                region.x -= 2;
                region.y -= 2;
                region.width += 4;
                region.height += 4;
                region.x = region.x < 0 ? 0 : region.x;
                region.y = region.y < 0 ? 0 : region.y;
                region.height = region.height > img1.height() ? img1.height() : region.height;*/
                Mat letter = new Mat(img1, region);
                letter = resize(letter, 20, 20);
                Imgcodecs.imwrite("c:\\ibcontroller\\2.png", letter);

                byte[] data = new byte[(int)(letter.total())];
                letter.get(0, 0, data);
                INDArray indarray = Nd4j.ones(letter.rows(),letter.cols());
                for (int i = 0; i < letter.rows(); i++) {
                    INDArray col = Nd4j.ones(letter.cols());
                    for (int j = 0; j < letter.cols(); j++) {
                        col.putScalar(j, data[i * letter.cols() + j] & 0xFF);
                    }
                    indarray.putRow(i, col);
                }
                indarray = Nd4j.expandDims(indarray, 0);
                indarray = Nd4j.expandDims(indarray, 0);
                long[] i = indarray.shape();
                int[] result = model.predict(indarray);
                sb.append(result[0]);
            }

            return sb.toString();
        } catch (Exception ex) {
            System.out.println(ex.getMessage());
            return "";
        }
    }

    static Mat resize(Mat image, int width, int height) {
        Mat resizedImg = new Mat();

        int w = image.width();
        int h = image.height();
        Size sz = new Size(width, height);

        // if the width is greater than the height then resize along the width
        if (w > h) {
            int new_h = h * width / w;
            sz.height = new_h;
        } else {
            int new_w = w * height / h;
            sz.width = new_w;
        }
        Imgproc.resize(image, resizedImg, sz);

        // determine the padding values for the width and height to
        // obtain the target dimensions
        double padW = (width - resizedImg.width()) / 2.0;
        double padH = (height- resizedImg.height()) / 2.0;

        // pad the image then apply one more resizing to handle any rounding issues
        Mat newImg = new Mat();
        Core.copyMakeBorder(resizedImg, newImg, (int)padH, (int)padH,(int)padW, (int)padW, Core.BORDER_REPLICATE);
        Imgproc.resize(newImg, resizedImg, new Size(width, height));

        return resizedImg;
    }
}