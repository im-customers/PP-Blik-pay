package main

import (
	oauth "PP-Blik-pay/server/oauth"
	"bytes"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"os/exec"

	"github.com/gorilla/mux"
)

const (
	PAYPAL_API_BASE = "https://api.sandbox.paypal.com" // Replace this with your actual API base URL
)

func indexHandler(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, "./static/index.html")
}

func capturePaymentHandler(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	orderID := vars["orderId"]

	access_token, err := oauth.GetAccessToken()
	if err != nil {
		http.Error(w, "Failed to get access token", http.StatusInternalServerError)
		return
	}

	fmt.Printf("🔍 Capturing payment for order %s\n", orderID)
	reqURL := fmt.Sprintf("%s/v2/checkout/orders/%s/capture", PAYPAL_API_BASE, orderID)

	req, err := http.NewRequest("POST", reqURL, nil)
	if err != nil {
		http.Error(w, "Failed to create request", http.StatusInternalServerError)
		return
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "application/json")
	req.Header.Set("Authorization", "Bearer "+access_token)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		http.Error(w, "Failed to make the request", http.StatusInternalServerError)
		return
	}
	defer resp.Body.Close()

	respBody, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		http.Error(w, "Failed to read response", http.StatusInternalServerError)
		return
	}

	fmt.Println("💰 Payment captured!")
	w.Header().Set("Content-Type", "application/json")
	w.Write(respBody)
}

type WebhookEvent struct {
	EventType string `json:"event_type"`
	Resource  struct {
		ID string `json:"id"`
	} `json:"resource"`
}

func webhookHandler(w http.ResponseWriter, r *http.Request) {
	accessToken, err := oauth.GetAccessToken()
	if err != nil {
		fmt.Println("⚠️  Webhook signature verification failed.")
		http.Error(w, "Failed to get access token", http.StatusInternalServerError)
		return
	}

	fmt.Println("🪝 Received Webhook Event")

	var webhookEvent WebhookEvent
	err = json.NewDecoder(r.Body).Decode(&webhookEvent)
	if err != nil {
		fmt.Println("⚠️  Failed to parse request body")
		http.Error(w, "Failed to parse request body", http.StatusBadRequest)
		return
	}

	// Verify the webhook signature
	verifyData := map[string]interface{}{
		"transmission_id":   r.Header.Get("paypal-transmission-id"),
		"transmission_time": r.Header.Get("paypal-transmission-time"),
		"cert_url":          r.Header.Get("paypal-cert-url"),
		"auth_algo":         r.Header.Get("paypal-auth-algo"),
		"transmission_sig":  r.Header.Get("paypal-transmission-sig"),
		"webhook_id":        oauth.WEBHOOK_ID,
		"webhook_event":     webhookEvent,
	}

	verifyDataJSON, err := json.Marshal(verifyData)
	if err != nil {
		fmt.Println("⚠️  Failed to serialize verify data")
		http.Error(w, "Failed to verify webhook signature", http.StatusBadRequest)
		return
	}

	reqURL := fmt.Sprintf("%s/v1/notifications/verify-webhook-signature", PAYPAL_API_BASE)
	req, err := http.NewRequest("POST", reqURL, bytes.NewBuffer(verifyDataJSON))
	if err != nil {
		fmt.Println("⚠️  Failed to create verify request")
		http.Error(w, "Failed to verify webhook signature", http.StatusInternalServerError)
		return
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "application/json")
	req.Header.Set("Authorization", "Bearer "+accessToken)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		fmt.Println("⚠️  Webhook signature verification failed.")
		http.Error(w, "Failed to verify webhook signature", http.StatusInternalServerError)
		return
	}
	defer resp.Body.Close()

	respBody, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		fmt.Println("⚠️  Failed to read verification response")
		http.Error(w, "Failed to verify webhook signature", http.StatusInternalServerError)
		return
	}

	var verificationResponse map[string]interface{}
	err = json.Unmarshal(respBody, &verificationResponse)
	if err != nil {
		fmt.Println("⚠️  Failed to parse verification response")
		http.Error(w, "Failed to verify webhook signature", http.StatusInternalServerError)
		return
	}

	verificationStatus, ok := verificationResponse["verification_status"].(string)
	if !ok || verificationStatus != "SUCCESS" {
		fmt.Println("⚠️  Webhook signature verification failed.")
		http.Error(w, "Failed to verify webhook signature", http.StatusBadRequest)
		return
	}

	// Capture the order if event_type is CHECKOUT.ORDER.APPROVED
	if webhookEvent.EventType == "CHECKOUT.ORDER.APPROVED" {
		captureURL := fmt.Sprintf("%s/v2/checkout/orders/%s/capture", PAYPAL_API_BASE, webhookEvent.Resource.ID)
		captureReq, err := http.NewRequest("POST", captureURL, nil)
		if err != nil {
			fmt.Println("❌ Payment failed.")
			http.Error(w, "Failed to capture the order", http.StatusInternalServerError)
			return
		}

		captureReq.Header.Set("Content-Type", "application/json")
		captureReq.Header.Set("Accept", "application/json")
		captureReq.Header.Set("Authorization", "Bearer "+accessToken)

		captureResp, err := client.Do(captureReq)
		if err != nil {
			fmt.Println("❌ Payment failed.")
			http.Error(w, "Failed to capture the order", http.StatusInternalServerError)
			return
		}
		defer captureResp.Body.Close()

		fmt.Println("💰 Payment captured!")
	}

	w.WriteHeader(http.StatusOK)
}

func main() {
	r := mux.NewRouter()

	r.HandleFunc("/static/index.js", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/javascript")
		http.ServeFile(w, r, "static/index.js")
	})

	r.HandleFunc("/static/styles.css", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "text/css")
		http.ServeFile(w, r, "static/styles.css")
	})

	r.HandleFunc("/", indexHandler).Methods("GET")
	r.HandleFunc("/capture/{orderId}", capturePaymentHandler).Methods("POST")
	r.HandleFunc("/webhook", webhookHandler).Methods("POST")

	port := os.Getenv("PORT")
	if port == "" {
		port = "3000"
	}

	cmd := exec.Command("open", fmt.Sprintf("http://localhost:%s", port))
	cmd.Start()

	log.Printf("Example app listening at http://localhost:%s\n", port)
	log.Fatal(http.ListenAndServe(":"+port, r))
}
