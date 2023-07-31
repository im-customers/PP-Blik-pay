package oauth

import (
	"bytes"
	"encoding/base64"
	"fmt"
	"io/ioutil"
	"net/http"
	"net/url"
	"os"

	"github.com/joho/godotenv"
)

var (
	isProd          bool
	PAYPAL_API_BASE string
	CLIENT_ID       string
	APP_SECRET      string
	WEBHOOK_ID      string
)

func init() {
	err := godotenv.Load()
	if err != nil {
		fmt.Println("Error loading .env file:", err)
	}

	isProd = os.Getenv("NODE_ENV") == "production"
	PAYPAL_API_BASE = getPaypalAPIBase()
	CLIENT_ID = os.Getenv("CLIENT_ID")
	APP_SECRET = os.Getenv("APP_SECRET")
}

func getPaypalAPIBase() string {
	if isProd {
		return "https://api.paypal.com"
	}
	return "https://api.sandbox.paypal.com"
}

func GetAccessToken() (string, error) {
	credentials := base64.StdEncoding.EncodeToString([]byte(fmt.Sprintf("%s:%s", CLIENT_ID, APP_SECRET)))

	data := url.Values{}
	data.Set("grant_type", "client_credentials")

	req, err := http.NewRequest("POST", fmt.Sprintf("%s/v1/oauth2/token", PAYPAL_API_BASE), bytes.NewBufferString(data.Encode()))
	if err != nil {
		return "", err
	}

	req.Header.Set("Accept", "application/json")
	req.Header.Set("Authorization", fmt.Sprintf("Basic %s", credentials))
	req.Header.Set("Content-Type", "application/x-www-form-urlencoded")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	respBody, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		fmt.Printf("⚠️ Error in generating access token")
		return "", err
	}
	fmt.Printf("🔍 access token: ", respBody)

	return string(respBody), nil
}

// func main() {
// 	accessToken, err := GetAccessToken()
// 	if err != nil {
// 		fmt.Println("Error:", err)
// 		return
// 	}
// 	fmt.Println("Access Token:", accessToken)
// }
