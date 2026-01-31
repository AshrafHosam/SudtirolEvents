import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { 
  Location, 
  WeatherResponse, 
  RecommendationResponse,
  WeatherClassification 
} from '../../models/api.models';

@Component({
  selector: 'app-direct',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './direct.component.html',
  styleUrls: ['./direct.component.css']
})
export class DirectComponent implements OnInit {
  locations: Location[] = [];
  selectedLocationId: number | null = null;
  selectedDate: string = '';
  customCity: string = '';
  useCustomCity: boolean = false;
  
  weatherData: WeatherResponse | null = null;
  recommendationData: RecommendationResponse | null = null;
  
  isLoadingWeather: boolean = false;
  isLoadingRecommendations: boolean = false;
  error: string | null = null;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadLocations();
    this.selectedDate = this.formatDateForInput(new Date());
  }

  loadLocations(): void {
    this.apiService.getLocations().subscribe({
      next: (locations) => {
        this.locations = locations;
        if (locations.length > 0) {
          this.selectedLocationId = locations[0].id;
        }
      },
      error: (err) => {
        this.error = 'Failed to load locations: ' + err.message;
      }
    });
  }

  fetchData(): void {
    this.error = null;
    this.weatherData = null;
    this.recommendationData = null;

    const date = this.selectedDate ? new Date(this.selectedDate) : undefined;

    if (this.useCustomCity && this.customCity.trim()) {
      this.fetchByCity(this.customCity.trim(), date);
    } else if (this.selectedLocationId) {
      this.fetchByLocationId(this.selectedLocationId, date);
    } else {
      this.error = 'Please select a location or enter a city name';
    }
  }

  private fetchByCity(city: string, date?: Date): void {
    this.isLoadingWeather = true;
    this.isLoadingRecommendations = true;

    // Fetch weather
    this.apiService.getWeatherByCity(city, date).subscribe({
      next: (data) => {
        this.weatherData = data;
        this.isLoadingWeather = false;
      },
      error: (err) => {
        this.error = 'Weather: ' + err.message;
        this.isLoadingWeather = false;
      }
    });

    // Fetch recommendations
    this.apiService.getRecommendationsByCity(city, date).subscribe({
      next: (data) => {
        this.recommendationData = data;
        this.isLoadingRecommendations = false;
      },
      error: (err) => {
        if (!this.error) {
          this.error = 'Recommendations: ' + err.message;
        }
        this.isLoadingRecommendations = false;
      }
    });
  }

  private fetchByLocationId(locationId: number, date?: Date): void {
    const location = this.locations.find(l => l.id === locationId);
    if (!location) return;

    this.isLoadingWeather = true;
    this.isLoadingRecommendations = true;

    // Fetch weather by coordinates
    this.apiService.getWeatherByCoordinates(location.latitude, location.longitude, date).subscribe({
      next: (data) => {
        this.weatherData = data;
        this.isLoadingWeather = false;
      },
      error: (err) => {
        this.error = 'Weather: ' + err.message;
        this.isLoadingWeather = false;
      }
    });

    // Fetch recommendations
    this.apiService.getRecommendations(locationId, date).subscribe({
      next: (data) => {
        this.recommendationData = data;
        this.isLoadingRecommendations = false;
      },
      error: (err) => {
        if (!this.error) {
          this.error = 'Recommendations: ' + err.message;
        }
        this.isLoadingRecommendations = false;
      }
    });
  }

  toggleCustomCity(): void {
    this.useCustomCity = !this.useCustomCity;
    if (!this.useCustomCity) {
      this.customCity = '';
    }
  }

  setToday(): void {
    this.selectedDate = this.formatDateForInput(new Date());
  }

  setTomorrow(): void {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    this.selectedDate = this.formatDateForInput(tomorrow);
  }

  private formatDateForInput(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  getClassificationBadgeClass(classification: WeatherClassification | string): string {
    switch (classification) {
      case 'Good': return 'badge-good';
      case 'Bad': return 'badge-bad';
      case 'Rainy': return 'badge-rainy';
      case 'Cold': return 'badge-cold';
      case 'Hot': return 'badge-hot';
      case 'Windy': return 'badge-windy';
      default: return '';
    }
  }

  get isLoading(): boolean {
    return this.isLoadingWeather || this.isLoadingRecommendations;
  }
}
