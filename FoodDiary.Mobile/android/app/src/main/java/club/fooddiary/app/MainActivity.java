package club.fooddiary.app;

import android.os.Handler;
import android.os.Looper;
import android.os.Bundle;
import android.os.SystemClock;
import android.graphics.Color;
import android.graphics.Typeface;
import android.view.Gravity;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebView;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;
import com.getcapacitor.BridgeActivity;
import com.getcapacitor.WebViewListener;

public class MainActivity extends BridgeActivity {
    private final Handler mainHandler = new Handler(Looper.getMainLooper());
    private View splashOverlay;

    public MainActivity() {
        bridgeBuilder.addWebViewListener(
            new WebViewListener() {
                @Override
                public void onPageLoaded(WebView webView) {
                    scheduleWebViewRedraw();
                    scheduleSplashOverlayHide();
                }
            }
        );
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        showSplashOverlay();
    }

    @Override
    public void onResume() {
        super.onResume();
        scheduleWebViewRedraw();
    }

    private void scheduleWebViewRedraw() {
        requestWebViewRedrawAfter(100);
        requestWebViewRedrawAfter(500);
        requestWebViewRedrawAfter(1000);
        requestWebViewRedrawAfter(2000);
    }

    private void requestWebViewRedrawAfter(long delayMillis) {
        mainHandler.postDelayed(
            () -> {
                if (getBridge() == null) {
                    return;
                }

                WebView webView = getBridge().getWebView();
                webView.getSettings().setOffscreenPreRaster(true);
                webView.setVisibility(View.VISIBLE);
                webView.requestFocus();
                webView.requestLayout();
                webView.invalidate();
                webView.postInvalidateOnAnimation();
                webView.evaluateJavascript(
                    "window.dispatchEvent(new Event('resize'));" +
                        "document.documentElement.style.opacity='0.999';" +
                        "document.body.style.transform='translateZ(0)';" +
                        "requestAnimationFrame(function(){" +
                        "document.documentElement.style.opacity='1';" +
                        "document.body.style.transform='';" +
                        "});",
                    null
                );

                if (webView.getParent() instanceof View parentView) {
                    parentView.requestLayout();
                    parentView.invalidate();
                }
            },
            delayMillis
        );
    }

    private void showSplashOverlay() {
        FrameLayout contentRoot = findViewById(android.R.id.content);
        FrameLayout overlay = new FrameLayout(this);
        overlay.setBackgroundColor(Color.WHITE);
        overlay.setClickable(true);
        overlay.setFocusable(true);

        LinearLayout content = new LinearLayout(this);
        content.setGravity(Gravity.CENTER);
        content.setOrientation(LinearLayout.VERTICAL);

        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.food_diary_splash_icon);
        logo.setScaleType(ImageView.ScaleType.CENTER_INSIDE);

        float density = getResources().getDisplayMetrics().density;
        int logoSize = (int) (132 * density);
        LinearLayout.LayoutParams logoParams = new LinearLayout.LayoutParams(logoSize, logoSize);
        content.addView(logo, logoParams);

        TextView brand = new TextView(this);
        brand.setGravity(Gravity.CENTER);
        brand.setText("Food Diary");
        brand.setTextColor(Color.rgb(37, 99, 235));
        brand.setTextSize(30);
        brand.setTypeface(Typeface.DEFAULT, Typeface.BOLD);

        LinearLayout.LayoutParams brandParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WRAP_CONTENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        );
        brandParams.topMargin = (int) (20 * density);
        content.addView(brand, brandParams);

        FrameLayout.LayoutParams contentParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WRAP_CONTENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        );
        contentParams.gravity = Gravity.CENTER;
        overlay.addView(content, contentParams);

        contentRoot.addView(
            overlay,
            new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
        );
        splashOverlay = overlay;
    }

    private void scheduleSplashOverlayHide() {
        mainHandler.postDelayed(this::hideSplashOverlay, 900);
    }

    private void hideSplashOverlay() {
        View overlay = splashOverlay;
        if (overlay == null) {
            return;
        }

        overlay
            .animate()
            .alpha(0f)
            .setDuration(180)
            .withEndAction(
                () -> {
                    if (overlay.getParent() instanceof ViewGroup parent) {
                        parent.removeView(overlay);
                    }
                    if (splashOverlay == overlay) {
                        splashOverlay = null;
                    }
                    refreshWebViewLayer();
                    scheduleWebViewRedraw();
                }
            )
            .start();
    }

    private void refreshWebViewLayer() {
        if (getBridge() == null) {
            return;
        }

        WebView webView = getBridge().getWebView();
        webView.setLayerType(View.LAYER_TYPE_SOFTWARE, null);
        webView.invalidate();
        dispatchRedrawPulse(webView);
        mainHandler.postDelayed(
            () -> {
                webView.setLayerType(View.LAYER_TYPE_HARDWARE, null);
                webView.requestLayout();
                webView.invalidate();
                webView.postInvalidateOnAnimation();
                dispatchRedrawPulse(webView);
            },
            120
        );
        mainHandler.postDelayed(() -> dispatchRedrawPulse(webView), 400);
    }

    private void dispatchRedrawPulse(WebView webView) {
        long eventTime = SystemClock.uptimeMillis();
        float x = webView.getWidth() / 2f;
        float y = webView.getHeight() / 2f;
        MotionEvent downEvent = MotionEvent.obtain(eventTime, eventTime, MotionEvent.ACTION_DOWN, x, y, 0);
        MotionEvent upEvent = MotionEvent.obtain(eventTime, eventTime + 16, MotionEvent.ACTION_UP, x, y, 0);

        webView.dispatchTouchEvent(downEvent);
        webView.dispatchTouchEvent(upEvent);
        downEvent.recycle();
        upEvent.recycle();
    }
}
